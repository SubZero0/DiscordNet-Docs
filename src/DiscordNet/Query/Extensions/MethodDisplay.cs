using Discord;
using DiscordNet.Github;
using DiscordNet.Handlers;
using DiscordNet.Query.Results;
using DiscordNet.Query.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DiscordNet.Query
{
    public partial class ResultDisplay
    {
        private async Task<EmbedBuilder> ShowMethodsAsync(EmbedBuilder eb, EmbedAuthorBuilder eab, IEnumerable<MethodInfoWrapper> list)
        {
            MethodInfoWrapper first = list.First();
            DocsHttpResult result;
            string pageUrl = SanitizeDocsUrl($"{first.Parent.TypeInfo.Namespace}.{first.Parent.TypeInfo.Name}");
            try
            {
                result = await GetWebDocsAsync($"{QueryHandler.DocsBaseUrl}api/{pageUrl}.html", first);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                result = new DocsHttpResult($"{QueryHandler.DocsBaseUrl}api/{pageUrl}.html{MethodToDocs(first)}");
            }
            eab.Name = $"Method: {first.Parent.TypeInfo.Namespace}.{first.Parent.DisplayName}.{first.Method.Name}";
            eab.Url = result.Url;
            eb.AddField((x) =>
            {
                x.IsInline = true;
                x.Name = "Docs:";
                x.Value = FormatDocsUrl(eab.Url);
            });
            var githubUrl = await GithubRest.GetMethodUrlAsync(first);
            if (githubUrl != null)
            {
                eb.AddField((x) =>
                {
                    x.IsInline = true;
                    x.Name = "Source:";
                    x.Value = FormatGithubUrl(githubUrl);
                });
            }
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
                x.Value = String.Join("\n", list.OrderBy(y => IsInherited(y)).Select(y => $"``{i++}-``{(IsInherited(y) ? " (i)" : "")} {BuildMethod(y.Method)}"));
            });
            return eb;
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
            string final = $"#{mi.Parent.TypeInfo.Namespace.Replace('.', '_')}_{mi.Parent.TypeInfo.Name}_{mi.Method.Name}_{parameters}";
            if (final.Length > 68 && !removeDiscord) //This isnt how they select if they should remove the namespace...
                return MethodToDocs(mi, true);
            return final;
        }

        private string BuildMethod(MethodInfo mi)
        {
            IEnumerable<string> parameters;
            if (mi.IsDefined(typeof(ExtensionAttribute)))
                parameters = mi.GetParameters().Skip(1).Select(x => $"{Utils.BuildType(x.ParameterType)} {x.Name}{GetParameterDefaultValue(x)}");
            else
                parameters = mi.GetParameters().Select(x => $"{BuildPreParameter(x)}{Utils.BuildType(x.ParameterType)} {x.Name}{GetParameterDefaultValue(x)}");
            return $"{Utils.BuildType(mi.ReturnType)} {mi.Name}({String.Join(", ", parameters)})";
        }

        private string BuildPreParameter(ParameterInfo pi)
        {
            if (pi.IsOut)
                return "out ";
            return "";
        }

        private string GetParameterDefaultValue(ParameterInfo pi)
        {
            if (pi.HasDefaultValue)
                return $" = {GetDefaultValueAsString(pi.DefaultValue)}";
            return "";
        }

        private string GetDefaultValueAsString(object obj)
        {
            if (obj == null)
                return "null";
            switch (obj)
            {
                case false: return "false";
                case true: return "true";
                default: return obj.ToString();
            }
        }
    }
}
