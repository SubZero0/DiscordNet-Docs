using Discord;
using DiscordNet.Query.Results;
using DiscordNet.Query.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DiscordNet.Query.Extensions
{
    public static class MethodDisplay
    {
        public static async Task<EmbedBuilder> ShowMethodsAsync(EmbedBuilder eb, EmbedAuthorBuilder eab, IEnumerable<MethodInfoWrapper> list)
        {
            MethodInfoWrapper first = list.First();
            DocsHttpResult result;
            try
            {
                result = await BaseDisplay.GetWebDocsAsync($"https://discord.foxbot.me/docs/api/{first.Parent.Namespace}.{first.Parent.Name}.html", first);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                result = new DocsHttpResult($"https://discord.foxbot.me/docs/api/{first.Parent.Namespace}.{first.Parent.Name}.html{MethodToDocs(first)}");
            }
            eab.Name = $"Method: {first.Parent.Namespace}.{first.Parent.Name}.{first.Method.Name}";
            eab.Url = result.Url;
            eb.AddField((x) =>
            {
                x.IsInline = false;
                x.Name = "Docs:";
                x.Value = result.Url;
            });
            if (result.Summary != null)
                eb.AddField((x) =>
                {
                    x.IsInline = false;
                    x.Name = "Summary:";
                    x.Value = result.Summary;
                });
            if (result.Example != null)
                eb.AddField((x) =>
                {
                    x.IsInline = false;
                    x.Name = "Example:";
                    x.Value = result.Example;
                });
            int i = 1;
            eb.AddField((x) =>
            {
                x.IsInline = false;
                x.Name = "Overloads:";
                x.Value = String.Join("\n", list.OrderBy(y => BaseDisplay.IsInherited(y)).Select(y => $"``{i++}-``{(BaseDisplay.IsInherited(y) ? " (i)" : "")} {BuildMethod(y.Method)}"));
            });
            /*var tips = ShowMethodsTips(list.Select(x => x.Method));
            i = 1;
            if (tips.Count != 0)
                eb.AddField((x) =>
                {
                    x.IsInline = false;
                    x.Name = "Tips & examples:";
                    x.Value = String.Join("\n", tips.Select(y => $"``{i++}-`` {y}"));
                });*/
            return eb;
        }

        private static List<string> ShowMethodsTips(IEnumerable<MethodInfo> list)
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

        private static string MethodToDocs(MethodInfoWrapper mi, bool removeDiscord = false) //Always second option because the docs urls are too strange, removing the namespace one time and not another...
        {
            if (BaseDisplay.IsInherited(mi))
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

        private static bool CompareTypes(Type t1, Type t2)
        {
            return t1.FullName.StartsWith(t2.FullName);
        }

        private static string BuildMethod(MethodInfo mi)
        {
            IEnumerable<string> parameters;
            if (mi.IsDefined(typeof(ExtensionAttribute)))
                parameters = mi.GetParameters().Skip(1).Select(x => $"{BaseDisplay.BuildType(x.ParameterType)} {x.Name}{GetParameterDefaultValue(x)}");
            else
                parameters = mi.GetParameters().Select(x => $"{BuildPreParameter(x)}{BaseDisplay.BuildType(x.ParameterType)} {x.Name}{GetParameterDefaultValue(x)}");
            return $"{BaseDisplay.BuildType(mi.ReturnType)} {mi.Name}({String.Join(", ", parameters)})";
        }

        private static string BuildPreParameter(ParameterInfo pi)
        {
            if (pi.IsOut)
                return "out ";
            return "";
        }

        private static string GetParameterDefaultValue(ParameterInfo pi)
        {
            if (pi.HasDefaultValue)
                return $" = {pi.DefaultValue?.ToString() ?? "null"}";
            return "";
        }
    }
}
