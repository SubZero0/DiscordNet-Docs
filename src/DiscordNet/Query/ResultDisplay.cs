using Discord;
using DiscordNet.Query.Extensions;
using DiscordNet.Query.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DiscordNet.Query
{
    public class ResultDisplay : MethodDisplay
    {
        private SearchResult<object> _result;
        public ResultDisplay(SearchResult<object> result)
        {
            _result = result;
        }

        public async Task<EmbedBuilder> Run()
        {
            var list = _result.List.GroupBy(x => GetPath(x));
            if (list.Count() == 1)
                return await Show(list.ElementAt(0));
            else
                return ShowMultiple(list.Select(x => x.First()));
        }

        internal async Task<EmbedBuilder> Show(IEnumerable<object> o)
        {
            var first = o.First();
            EmbedAuthorBuilder eab = new EmbedAuthorBuilder();
            eab.IconUrl = "http://i.imgur.com/XW4RU5e.png";
            EmbedBuilder eb = new EmbedBuilder().WithAuthor(eab);
            if (first is TypeInfo)
            {
                TypeInfo r = (TypeInfo)o.First();
                eab.Name = $"Type: {r.Namespace}.{r.Name}";
                eab.Url = $"https://discord.foxbot.me/docs/api/{r.Namespace}.{r.Name}.html";
                eb.Description = $"Docs: {eab.Url}";
            }
            else if (first is MethodInfo)
            {
                MethodInfo r = (MethodInfo)o.First();
                string link;
                try
                {
                    link = await GetMethodInDocs($"https://discord.foxbot.me/docs/api/{r.DeclaringType.Namespace}.{r.DeclaringType.Name}.html", r);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                    link = $"https://discord.foxbot.me/docs/api/{r.DeclaringType.Namespace}.{r.DeclaringType.Name}.html{MethodToDocs(r)}";
                }
                eab.Name = $"Method: {r.DeclaringType.Namespace}.{r.DeclaringType.Name}.{r.Name}";
                eab.Url = link;
                eb.Description = $"**Docs:** {link}\n\n{ShowMethods(o.Select(x => (MethodInfo)x))}";
            }
            else if(first is PropertyInfo)
            {
                PropertyInfo r = (PropertyInfo)o.First();
                eab.Name = $"Property: {r.DeclaringType.Namespace}.{r.DeclaringType.Name}.{r.Name}";
                eab.Url = $"https://discord.foxbot.me/docs/api/{r.DeclaringType.Namespace}.{r.DeclaringType.Name}.html{PropertyToDocs(r)}";
                eb.Description = $"Docs: {eab.Url}";
            }
            return eb;
        }

        internal EmbedBuilder ShowMultiple(IEnumerable<object> obj)
        {
            EmbedBuilder eb = new EmbedBuilder();
            if (obj.Count() > 10)
            {
                eb.Title = "Too many results, try filtering your search. Some results:";
                eb.Description = String.Join("\n", GetPaths(obj.Take(10)));
            }
            else
            {
                eb.Title = "Did you mean:";
                eb.Description = String.Join("\n", GetPaths(obj));
                eb.Footer = new EmbedFooterBuilder().WithText("Try looking at ``help`` to filter more your query.");
            }
            return eb;
        }

        internal async Task<string> GetMethodInDocs(string url, MethodInfo mi)
        {
            string search = $"{mi.DeclaringType.Namespace}_{mi.DeclaringType.Name}_{mi.Name}_".Replace('.', '_');
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

        internal string MethodToDocs(MethodInfo mi, bool removeDiscord = false) //Always second option because the docs urls are too strange, removing the namespace one time and not another...
        {
            Regex rgx = new Regex("[^a-zA-Z0-9_][^a-zA-Z]*");
            string parameters = "";
            string parameters_orig = "";
            foreach (ParameterInfo pi in mi.GetParameters())
            {
                string format = rgx.Replace(pi.ParameterType.ToString(), "_").Replace("System_Action", "Action").Replace("System_Collections_Generic_IEnumerable", "IEnumerable");
                if (removeDiscord)
                    format = format.Replace("Discord_", "");
                parameters += $"{format}_";
                parameters_orig += $"{pi.ParameterType.ToString()}_";
            }
            string final = $"#{mi.DeclaringType.Namespace.Replace('.', '_')}_{mi.DeclaringType.Name}_{mi.Name}_{parameters}";
            if (final.Length > 68 && !removeDiscord) //This isnt how they select if they should remove the namespace...
                return MethodToDocs(mi, true);
            return final;
        }

        internal string PropertyToDocs(PropertyInfo pi)
        {
            return $"#{pi.DeclaringType.Namespace.Replace('.', '_')}_{pi.DeclaringType.Name}_{pi.Name}";
        }

        internal List<string> GetPaths(IEnumerable<object> list)
        {
            List<string> newlist = new List<string>();
            foreach(object o in list)
                newlist.Add(GetPath(o));
            return newlist;
        }

        internal string GetPath(object o)
        {
            if (o is TypeInfo)
            {
                TypeInfo r = (TypeInfo)o;
                return $"Type: {r.Name} in {r.Namespace}";
            }
            if (o is MethodInfo)
            {
                MethodInfo r = (MethodInfo)o;
                return $"Method: {r.Name} in {r.DeclaringType.Namespace}.{r.DeclaringType.Name}";
            }
            if (o is PropertyInfo)
            {
                PropertyInfo r = (PropertyInfo)o;
                return $"Property: {r.Name} in {r.DeclaringType.Namespace}.{r.DeclaringType.Name}";
            }
            return o.GetType().ToString();
        }
    }
}
