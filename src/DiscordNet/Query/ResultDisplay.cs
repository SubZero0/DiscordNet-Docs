using Discord;
using DiscordNet.Query.Results;
using DiscordNet.Query.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DiscordNet.Query
{
    public class ResultDisplay
    {
        private SearchResult<object> _result;
        private Cache _cache;
        public ResultDisplay(SearchResult<object> result, Cache cache)
        {
            _result = result;
            _cache = cache;
        }

        public async Task<EmbedBuilder> RunAsync()
        {
            var list = _result.List.GroupBy(x => GetPath(x, false));
            if (list.Count() == 1)
                return await ShowAsync(list.ElementAt(0));
            else
                return ShowMultiple(list.Select(x => x.First()));
        }

        private async Task<EmbedBuilder> ShowAsync(IEnumerable<object> o)
        {
            var first = o.First();
            EmbedAuthorBuilder eab = new EmbedAuthorBuilder();
            eab.IconUrl = "http://i.imgur.com/XW4RU5e.png";
            EmbedBuilder eb = new EmbedBuilder().WithAuthor(eab);
            if (first is TypeInfo)
            {
                TypeInfo r = (TypeInfo)first;
                eab.Name = $"{(r.IsInterface ? "Interface" : "Type")}: {r.Namespace}.{r.Name}";
                eab.Url = $"https://discord.foxbot.me/docs/api/{r.Namespace}.{r.Name}.html";
                eb.AddField((x) =>
                {
                    x.IsInline = false;
                    x.Name = "Docs:";
                    x.Value = eab.Url;
                });
                ShowTypes(eb, o.Select(x => (TypeInfo)x));
            }
            else if (first is MethodInfoWrapper)
            {
                MethodInfoWrapper r = (MethodInfoWrapper)first;
                string link;
                try
                {
                    link = await GetMethodInDocsAsync($"https://discord.foxbot.me/docs/api/{r.Parent.Namespace}.{r.Parent.Name}.html", r);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                    link = $"https://discord.foxbot.me/docs/api/{r.Parent.Namespace}.{r.Parent.Name}.html{MethodToDocs(r)}";
                }
                eab.Name = $"Method: {r.Parent.Namespace}.{r.Parent.Name}.{r.Method.Name}";
                eab.Url = link;
                eb.AddField((x) =>
                {
                    x.IsInline = false;
                    x.Name = "Docs:";
                    x.Value = link;
                });
                ShowMethods(eb, o.Select(x => (MethodInfoWrapper)x));
            }
            else if(first is PropertyInfoWrapper)
            {
                PropertyInfoWrapper r = (PropertyInfoWrapper)first;
                eab.Name = $"Property: {r.Parent.Namespace}.{r.Parent.Name}.{r.Property.Name} {(IsInherited(r) ? "(I)" : "")}";
                eab.Url = $"https://discord.foxbot.me/docs/api/{r.Parent.Namespace}.{r.Parent.Name}.html{PropertyToDocs(r)}";
                eb.AddField((x) =>
                {
                    x.IsInline = false;
                    x.Name = "Docs:";
                    x.Value = eab.Url;
                });
                ShowProperties(eb, o.Select(x => (PropertyInfoWrapper)x));
            }
            return eb;
        }

        private EmbedBuilder ShowMultiple(IEnumerable<object> obj)
        {
            EmbedBuilder eb = new EmbedBuilder();
            if (obj.Count() > 10)
            {
                eb.Title = "Too many results, try filtering your search. Some results:";
                eb.Description = string.Join("\n", GetPaths(obj.Take(10)));
            }
            else
            {
                eb.Title = "Did you mean:";
                eb.Description = string.Join("\n", GetPaths(obj));
                eb.Footer = new EmbedFooterBuilder().WithText("Type help to see keywords to filter your query.");
            }
            return eb;
        }

        private async Task<string> GetMethodInDocsAsync(string url, MethodInfoWrapper mi)
        {
            if (IsInherited(mi))
                return url;
            string search = $"{mi.Parent.Namespace}_{mi.Parent.Name}_{mi.Method.Name}_".Replace('.', '_');
            string html;
            using (var httpClient = new HttpClient())
            {
                var res = await httpClient.GetAsync(url);
                if (!res.IsSuccessStatusCode)
                    throw new Exception("Not possible to connect to the docs page");
                html = await res.Content.ReadAsStringAsync();
            }
            html = html.Substring(html.IndexOf($"<h4 id=\"{search}"));
            html = html.Substring(html.IndexOf('"') + 1);
            html = html.Substring(0, html.IndexOf('"'));
            return $"{url}#{html}";
        }

        private string MethodToDocs(MethodInfoWrapper mi, bool removeDiscord = false) //Always second option because the docs urls are too strange, removing the namespace one time and not another...
        {
            if (IsInherited(mi))
                return "";
            Regex rgx = new Regex("[^a-zA-Z0-9_][^a-zA-Z]*");
            string parameters = "";
            string parameters_orig = "";
            foreach (ParameterInfo pi in mi.Method.GetParameters())
            {
                string format = rgx.Replace(pi.ParameterType.ToString(), "_").Replace("System_Action", "Action").Replace("System_Collections_Generic_IEnumerable", "IEnumerable");
                if (removeDiscord)
                    format = format.Replace("Discord_", "");
                parameters += $"{format}_";
                parameters_orig += $"{pi.ParameterType.ToString()}_";
            }
            string final = $"#{mi.Parent.Namespace.Replace('.', '_')}_{mi.Parent.Name}_{mi.Method.Name}_{parameters}";
            if (final.Length > 68 && !removeDiscord) //This isnt how they select if they should remove the namespace...
                return MethodToDocs(mi, true);
            return final;
        }

        private string PropertyToDocs(PropertyInfoWrapper pi)
        {
            if (IsInherited(pi))
                return "";
            return $"#{pi.Parent.Namespace.Replace('.', '_')}_{pi.Parent.Name}_{pi.Property.Name}";
        }

        private List<string> GetPaths(IEnumerable<object> list)
        {
            List<string> newlist = new List<string>();
            foreach(object o in list)
                newlist.Add(GetPath(o));
            return newlist;
        }

        private string GetPath(object o, bool withInheritanceMarkup = true)
        {
            if (o is TypeInfo)
            {
                TypeInfo r = (TypeInfo)o;
                return $"Type: {r.Name} in {r.Namespace}";
            }
            if (o is MethodInfoWrapper)
            {
                MethodInfoWrapper r = (MethodInfoWrapper)o;
                return $"Method: {r.Method.Name} in {r.Parent.Namespace}.{r.Parent.Name} {(IsInherited(r) && withInheritanceMarkup ? "(I)" : "")}";
            }
            if (o is PropertyInfoWrapper)
            {
                PropertyInfoWrapper r = (PropertyInfoWrapper)o;
                return $"Property: {r.Property.Name} in {r.Parent.Namespace}.{r.Parent.Name} {(IsInherited(r) && withInheritanceMarkup ? "(I)" : "")}";
            }
            return o.GetType().ToString();
        }


        //
        // MethodDisplay Section
        //
        private void ShowMethods(EmbedBuilder eb, IEnumerable<MethodInfoWrapper> list)
        {
            MethodInfoWrapper first = list.First();
            int i = 1;
            eb.AddField((x) =>
            {
                x.IsInline = false;
                x.Name = "Overloads:";
                x.Value = String.Join("\n", list.OrderBy(y => IsInherited(y)).Select(y => $"``{i++}-``{(IsInherited(y) ? " (I)" : "")} {BuildMethod(y.Method)}"));
            });
            var tips = ShowMethodsTips(list.Select(x => x.Method));
            i = 1;
            if (tips.Count != 0)
                eb.AddField((x) =>
                {
                    x.IsInline = false;
                    x.Name = "Tips & examples:";
                    x.Value = String.Join("\n", tips.Select(y => $"``{i++}-`` {y}"));
                });
        }

        private List<string> ShowMethodsTips(IEnumerable<MethodInfo> list)
        {
            List<string> tips = new List<string>();
            if (list.FirstOrDefault(x => CompareTypes(x.ReturnType, typeof(Action)) || x.GetParameters().FirstOrDefault(y => CompareTypes(y.ParameterType, typeof(Action))) != null) != null)
                tips.Add("Action [only execute]: ``(x) => x.Something``");
            if (list.FirstOrDefault(x => CompareTypes(x.ReturnType, typeof(Func<>)) || x.GetParameters().FirstOrDefault(y => CompareTypes(y.ParameterType, typeof(Func<>))) != null) != null)
                tips.Add("Func [execute and return]: ``(x) => x.Something``");
            if (list.FirstOrDefault(x => CompareTypes(x.ReturnType, typeof(Task)) || x.GetParameters().FirstOrDefault(y => CompareTypes(y.ParameterType, typeof(Task))) != null) != null)
                tips.Add("Task: always remember to ``await`` if needed.");
            return tips;
        }

        private bool CompareTypes(Type t1, Type t2)
        {
            return t1.FullName.StartsWith(t2.FullName);
        }

        private string BuildMethod(MethodInfo mi)
        {
            return $"{BuildType(mi.ReturnType)} {mi.Name}({String.Join(", ", mi.GetParameters().Select(x => $"{BuildType(x.ParameterType)} {x.Name}{GetParameterDefaultValue(x)}"))})";
        }

        private string GetParameterDefaultValue(ParameterInfo pi)
        {
            if (pi.HasDefaultValue)
                return $" = {pi.DefaultValue?.ToString() ?? "null"}";
            return "";
        }

        private bool IsInherited(MethodInfoWrapper mi) => $"{mi.Parent.Namespace}.{mi.Parent.Name}" != $"{mi.Method.DeclaringType.Namespace}.{mi.Method.DeclaringType.Name}";


        //
        // PropertyDisplay Section
        //
        private void ShowProperties(EmbedBuilder eb, IEnumerable<PropertyInfoWrapper> list)
        {
            PropertyInfoWrapper first = list.First();
            eb.AddField((x) =>
            {
                x.IsInline = true;
                x.Name = "Type:";
                x.Value = BuildType(first.Property.PropertyType);
            });
            eb.AddField((x) =>
            {
                x.IsInline = true;
                x.Name = "Get & Set:";
                x.Value = $"Can write: {first.Property.CanWrite}\nCan read: {first.Property.CanRead}";
            });
        }

        private bool IsInherited(PropertyInfoWrapper mi) => $"{mi.Parent.Namespace}.{mi.Parent.Name}" != $"{mi.Property.DeclaringType.Namespace}.{mi.Property.DeclaringType.Name}";


        //
        // TypeDisplay Section
        //
        private void ShowTypes(EmbedBuilder eb, IEnumerable<TypeInfo> list)
        {
            TypeInfo first = list.First();
            CacheBag cb = _cache.GetCacheBag(first);
            if (cb.Methods.Count != 0)
            {
                int now = 0, max = (3 < cb.Methods.Count ? 3 : cb.Methods.Count);
                Random r = new Random();
                Dictionary<int, MethodInfo> methods = new Dictionary<int, MethodInfo>();
                while(now != max)
                {
                    int n = r.Next(cb.Methods.Count);
                    while (methods.ContainsKey(n))
                    {
                        n++;
                        if (n == cb.Methods.Count)
                            n = 0;
                    }
                    methods[n] = cb.Methods.ElementAt(n);
                    now++;
                }
                int i = 1;
                eb.AddField((x) =>
                {
                    x.IsInline = true;
                    x.Name = $"Some methods ({max}/{cb.Methods.Count}):";
                    x.Value = String.Join("\n", methods.Values.Select(y => $"``{i++}-``{(IsInherited(new MethodInfoWrapper(first, y)) ? " (I)" : "")} {y.Name}(...)"));
                });
            }
            if (cb.Properties.Count != 0)
            {
                int now = 0, max = (3 < cb.Properties.Count ? 3 : cb.Properties.Count);
                Random r = new Random();
                Dictionary<int, PropertyInfo> properties = new Dictionary<int, PropertyInfo>();
                while (now != max)
                {
                    int n = r.Next(cb.Properties.Count);
                    while (properties.ContainsKey(n))
                    {
                        n++;
                        if (n == cb.Properties.Count)
                            n = 0;
                    }
                    properties[n] = cb.Properties.ElementAt(n);
                    now++;
                }
                int i = 1;
                eb.AddField((x) =>
                {
                    x.IsInline = true;
                    x.Name = $"Some properties ({max}/{cb.Properties.Count}):";
                    x.Value = String.Join("\n", properties.Values.Select(y => $"``{i++}-``{(IsInherited(new PropertyInfoWrapper(first, y)) ? " (I)" : "")} {y.Name}"));
                });
            }
        }


        //
        // Extras
        //
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
    }
}
