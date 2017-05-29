using DiscordNet.Query.Results;
using DiscordNet.Query.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DiscordNet.Query
{
    public partial class ResultDisplay
    {
        private async Task<DocsHttpResult> GetWebDocsAsync(string url, object o)
        {
            string summary = null, example = null;
            string search = GetDocsUrlPath(o);
            var result = await GetWebDocsHtmlAsync(url, o);
            string html = result.Item2;
            if (result.Item1 && !string.IsNullOrEmpty(html))
            {
                string block = ((o is TypeInfoWrapper) ? html.Substring(html.IndexOf($"<h1 id=\"{search}")) : html.Substring(html.IndexOf($"<h4 id=\"{search}")));
                string anchor = block.Substring(block.IndexOf('"') + 1);
                anchor = anchor.Substring(0, anchor.IndexOf('"'));
                summary = block.Substring(block.IndexOf("summary\">") + 9);
                summary = summary.Substring(0, summary.IndexOf("</div>"));
                summary = WebUtility.HtmlDecode(StripTags(summary));
                /*string example = block.Substring(block.IndexOf("example\">")); //TODO: Find this
                summary = summary.Substring(0, summary.IndexOf("</div>"));*/
                if (!(o is TypeInfoWrapper) && !IsInherited(o))
                    url += $"#{anchor}";
            }
            return new DocsHttpResult(url, summary, example);
        }

        private async Task<(bool, string)> GetWebDocsHtmlAsync(string url, object o)
        {
            string html;
            if (IsInherited(o))
            {
                if (o is MethodInfoWrapper)
                {
                    MethodInfoWrapper mi = (MethodInfoWrapper)o;
                    if (!mi.Method.DeclaringType.Namespace.StartsWith("Discord"))
                        return (true, "");
                    else
                        url = $"https://discord.foxbot.me/docs/api/{SanitizeDocsUrl($"{mi.Method.DeclaringType.Namespace}.{mi.Method.DeclaringType.Name}")}.html";
                }
                else
                {
                    PropertyInfoWrapper pi = (PropertyInfoWrapper)o;
                    if (!pi.Property.DeclaringType.Namespace.StartsWith("Discord"))
                        return (true, "");
                    else
                        url = $"https://discord.foxbot.me/docs/api/{SanitizeDocsUrl($"{pi.Property.DeclaringType.Namespace}.{pi.Property.DeclaringType.Name}")}.html";
                }
            }
            using (var httpClient = new HttpClient())
            {
                var res = await httpClient.GetAsync(url);
                if (!res.IsSuccessStatusCode)
                {
                    if (res.StatusCode != HttpStatusCode.NotFound)
                        //throw new Exception("Not possible to connect to the docs page");
                        return (false, "Not possible to connect to the docs page");
                    else
                        return (false, "Docs page not found");
                }
                html = await res.Content.ReadAsStringAsync();
            }
            return (true, html);
        }

        private string GetDocsUrlPath(object o)
        {
            bool useParent = !IsInherited(o);
            Regex rgx = new Regex("\\W+");
            if (o is TypeInfoWrapper)
            {
                TypeInfoWrapper r = (TypeInfoWrapper)o;
                return rgx.Replace($"{r.TypeInfo.Namespace}_{r.TypeInfo.Name}", "_");
            }
            if (o is MethodInfoWrapper)
            {
                MethodInfoWrapper r = (MethodInfoWrapper)o;
                return rgx.Replace($"{(useParent ? r.Parent.TypeInfo.Namespace : r.Method.DeclaringType.Namespace)}_{(useParent ? r.Parent.TypeInfo.Name : r.Method.DeclaringType.Name)}_{r.Method.Name}_", "_");
            }
            if (o is PropertyInfoWrapper)
            {
                PropertyInfoWrapper r = (PropertyInfoWrapper)o;
                return rgx.Replace($"{(useParent ? r.Parent.TypeInfo.Namespace : r.Property.DeclaringType.Namespace)}_{(useParent ? r.Parent.TypeInfo.Name : r.Property.DeclaringType.Name)}_{r.Property.Name}", "_");
            }
            if (o is EventInfoWrapper)
            {
                EventInfoWrapper r = (EventInfoWrapper)o;
                return rgx.Replace($"{r.Parent.TypeInfo.Namespace}_{r.Parent.TypeInfo.Name}_{r.Event.Name}".Replace('.', '_'), "_");
            }
            return rgx.Replace($"{o.GetType().Namespace}_{o.GetType().Name}".Replace('.', '_'), "_");
        }

        //Generic types will return like Type`1 and the docs change to Type-1
        private string SanitizeDocsUrl(string text)
        {
            return text.Replace('`', '-');
        }

        public static bool IsInherited(object o)
        {
            if (o is PropertyInfoWrapper)
            {
                var pi = (PropertyInfoWrapper)o;
                return $"{pi.Parent.TypeInfo.Namespace}.{pi.Parent.TypeInfo.Name}" != $"{pi.Property.DeclaringType.Namespace}.{pi.Property.DeclaringType.Name}";
            }
            if (o is MethodInfoWrapper)
            {
                var mi = (MethodInfoWrapper)o;
                return $"{mi.Parent.TypeInfo.Namespace}.{mi.Parent.TypeInfo.Name}" != $"{mi.Method.DeclaringType.Namespace}.{mi.Method.DeclaringType.Name}";
            }
            return false;
        }

        private List<string> GetPaths(IEnumerable<object> list)
        {
            List<string> newlist = new List<string>();
            foreach (object o in list)
                newlist.Add(GetPath(o));
            return newlist;
        }

        public static string GetPath(object o, bool withInheritanceMarkup = true)
        {
            if (o is TypeInfoWrapper)
            {
                TypeInfoWrapper r = (TypeInfoWrapper)o;
                string type = "Type";
                if (r.TypeInfo.IsInterface)
                    type = "Interface";
                else if (r.TypeInfo.IsEnum)
                    type = "Enum";
                return $"{type}: {r.DisplayName} in {r.TypeInfo.Namespace}";
            }
            if (o is MethodInfoWrapper)
            {
                MethodInfoWrapper r = (MethodInfoWrapper)o;
                return $"Method: {r.Method.Name} in {r.Parent.TypeInfo.Namespace}.{r.Parent.DisplayName}{(IsInherited(r) && withInheritanceMarkup ? " (i)" : "")}";
            }
            if (o is PropertyInfoWrapper)
            {
                PropertyInfoWrapper r = (PropertyInfoWrapper)o;
                return $"Property: {r.Property.Name} in {r.Parent.TypeInfo.Namespace}.{r.Parent.DisplayName}{(IsInherited(r) && withInheritanceMarkup ? " (i)" : "")}";
            }
            if (o is EventInfoWrapper)
            {
                EventInfoWrapper r = (EventInfoWrapper)o;
                return $"Event: {r.Event.Name} in {r.Parent.TypeInfo.Namespace}.{r.Parent.DisplayName}";
            }
            return o.GetType().ToString();
        }

        private string BuildType(Type type)
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

        private string StripTags(string source)
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
