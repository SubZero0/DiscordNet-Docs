using DiscordNet.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DiscordNet.Docs
{
    public class ResultDisplay
    {
        private SearchResult<object> _result;
        public ResultDisplay(SearchResult<object> result)
        {
            _result = result;
        }

        public async Task<string> Run()
        {
            if (_result.Count == 1)
                return await Show(_result.List[0]);
            else
                return ShowMultiple(_result.List);
        }

        internal async Task<string> Show(object o)
        {
            if (o is TypeInfo)
            {
                TypeInfo r = (TypeInfo)o;
                return $"Here: https://discord.foxbot.me/docs/api/{r.Namespace}.{r.Name}.html";
            }
            if (o is MethodInfo)
            {
                MethodInfo r = (MethodInfo)o;
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
                return $"Here: {link}";
            }
            if (o is PropertyInfo)
            {
                PropertyInfo r = (PropertyInfo)o;
                return $"Here: https://discord.foxbot.me/docs/api/{r.DeclaringType.Namespace}.{r.DeclaringType.Name}.html{PropertyToDocs(r)}";
            }
            return "???";
        }

        internal string ShowMultiple(List<object> obj)
        {
            if (obj.Count > 10)
                return $"**Too many results, try filtering your search. Some results:**\n{String.Join("\n", GetPaths(obj.Take(10)))}";
            return $"**Did you mean:**\n{String.Join("\n", GetPaths(obj))}\nTry looking at: ``docs help`` to filter more your query.";
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
            {
                if (o is TypeInfo)
                {
                    TypeInfo r = (TypeInfo)o;
                    newlist.Add($"Type: {r.Name} in {r.Namespace}");
                }
                if (o is MethodInfo)
                {
                    MethodInfo r = (MethodInfo)o;
                    newlist.Add($"Method: {r.Name} in {r.DeclaringType.Namespace}.{r.DeclaringType.Name}");
                }
                if (o is PropertyInfo)
                {
                    PropertyInfo r = (PropertyInfo)o;
                    newlist.Add($"Property: {r.Name} in {r.DeclaringType.Namespace}.{r.DeclaringType.Name}");
                }
            }
            return newlist;
        }
    }
}
