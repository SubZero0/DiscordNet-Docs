using DiscordNet.Query.Results;
using DiscordNet.Query.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

namespace DiscordNet.Query.Extensions
{
    public static class BaseDisplay
    {
        public static async Task<DocsHttpResult> GetWebDocsAsync(string url, object o)
        {
            string summary, example = null;
            string search = GetDocsUrlPath(o);
            string html = await GetWebDocsHtmlAsync(url, o);
            if (!string.IsNullOrEmpty(html))
            {
                string block = ((o is TypeInfo) ? html.Substring(html.IndexOf($"<h1 id=\"{search}")) : html.Substring(html.IndexOf($"<h4 id=\"{search}")));
                string anchor = block.Substring(block.IndexOf('"') + 1);
                anchor = anchor.Substring(0, anchor.IndexOf('"'));
                summary = block.Substring(block.IndexOf("summary\">") + 9);
                summary = summary.Substring(0, summary.IndexOf("</div>"));
                summary = StripTags(summary);
                /*string example = block.Substring(block.IndexOf("example\">")); //TODO: Find this
                summary = summary.Substring(0, summary.IndexOf("</div>"));*/
                if (!(o is TypeInfo) && !IsInherited(o))
                    url += $"#{anchor}";
            }
            else
                summary = null;
            return new DocsHttpResult(url, summary, example);
        }

        private static async Task<string> GetWebDocsHtmlAsync(string url, object o)
        {
            string html;
            if (IsInherited(o))
            {
                if (o is MethodInfoWrapper)
                {
                    MethodInfoWrapper mi = (MethodInfoWrapper)o;
                    if (!mi.Method.DeclaringType.Namespace.StartsWith("Discord"))
                        return "";
                    else
                        url = $"https://discord.foxbot.me/docs/api/{mi.Method.DeclaringType.Namespace}.{mi.Method.DeclaringType.Name}.html";
                }
                else
                {
                    PropertyInfoWrapper pi = (PropertyInfoWrapper)o;
                    if (!pi.Property.DeclaringType.Namespace.StartsWith("Discord"))
                        return "";
                    else
                        url = $"https://discord.foxbot.me/docs/api/{pi.Property.DeclaringType.Namespace}.{pi.Property.DeclaringType.Name}.html";
                }
            }
            using (var httpClient = new HttpClient())
            {
                var res = await httpClient.GetAsync(url);
                if (!res.IsSuccessStatusCode)
                    throw new Exception("Not possible to connect to the docs page");
                html = await res.Content.ReadAsStringAsync();
            }
            return html;
        }

        private static string GetDocsUrlPath(object o)
        {
            bool useParent = !IsInherited(o);
            if (o is TypeInfo)
            {
                TypeInfo r = (TypeInfo)o;
                return $"{r.Namespace}_{r.Name}".Replace('.', '_');
            }
            if (o is MethodInfoWrapper)
            {
                MethodInfoWrapper r = (MethodInfoWrapper)o;
                return $"{(useParent ? r.Parent.Namespace : r.Method.DeclaringType.Namespace)}_{(useParent ? r.Parent.Name : r.Method.DeclaringType.Name)}_{r.Method.Name}_".Replace('.', '_');
            }
            if (o is PropertyInfoWrapper)
            {
                PropertyInfoWrapper r = (PropertyInfoWrapper)o;
                return $"{(useParent ? r.Parent.Namespace : r.Property.DeclaringType.Namespace)}_{(useParent ? r.Parent.Name : r.Property.DeclaringType.Name)}_{r.Property.Name}".Replace('.', '_');
            }
            if (o is EventInfo)
            {
                EventInfo r = (EventInfo)o;
                return $"{r.DeclaringType.Namespace}_{r.DeclaringType.Name}_{r.Name}".Replace('.', '_');
            }
            return $"{o.GetType().Namespace}_{o.GetType().Name}".Replace('.', '_');
        }

        public static bool IsInherited(object o)
        {
            if (o is PropertyInfoWrapper)
            {
                var pi = (PropertyInfoWrapper)o;
                return $"{pi.Parent.Namespace}.{pi.Parent.Name}" != $"{pi.Property.DeclaringType.Namespace}.{pi.Property.DeclaringType.Name}";
            }
            if (o is MethodInfoWrapper)
            {
                var mi = (MethodInfoWrapper)o;
                return $"{mi.Parent.Namespace}.{mi.Parent.Name}" != $"{mi.Method.DeclaringType.Namespace}.{mi.Method.DeclaringType.Name}";
            }
            return false;
        }

        public static List<string> GetPaths(IEnumerable<object> list)
        {
            List<string> newlist = new List<string>();
            foreach (object o in list)
                newlist.Add(GetPath(o));
            return newlist;
        }

        public static string GetPath(object o, bool withInheritanceMarkup = true)
        {
            if (o is TypeInfo)
            {
                TypeInfo r = (TypeInfo)o;
                return $"Type: {r.Name} in {r.Namespace}";
            }
            if (o is MethodInfoWrapper)
            {
                MethodInfoWrapper r = (MethodInfoWrapper)o;
                return $"Method: {r.Method.Name} in {r.Parent.Namespace}.{r.Parent.Name}{(IsInherited(r) && withInheritanceMarkup ? " (i)" : "")}";
            }
            if (o is PropertyInfoWrapper)
            {
                PropertyInfoWrapper r = (PropertyInfoWrapper)o;
                return $"Property: {r.Property.Name} in {r.Parent.Namespace}.{r.Parent.Name}{(IsInherited(r) && withInheritanceMarkup ? " (i)" : "")}";
            }
            if (o is EventInfo)
            {
                EventInfo r = (EventInfo)o;
                return $"Event: {r.Name} in {r.DeclaringType.Namespace}.{r.DeclaringType.Name}";
            }
            return o.GetType().ToString();
        }

        public static string BuildType(Type type)
        {
            string name = type.Name;
            int idx;
            if ((idx = name.IndexOf('`')) != -1)
                name = name.Substring(0, idx);
            string generic = "";
            if (type.IsConstructedGenericType)
                generic = $"<{String.Join(", ", type.GetGenericArguments().Select(x => BuildType(x)))}>";
            return $"{name}{generic}";
        }

        public static string StripTags(string source)
        {
            char[] array = new char[source.Length];
            int arrayIndex = 0;
            bool inside = false;

            for (int i = 0; i < source.Length; i++)
            {
                char let = source[i];
                if (let == '<')
                {
                    inside = true;
                    continue;
                }
                if (let == '>')
                {
                    inside = false;
                    continue;
                }
                if (!inside)
                {
                    array[arrayIndex] = let;
                    arrayIndex++;
                }
            }
            return new string(array, 0, arrayIndex);
        }
    }
}
