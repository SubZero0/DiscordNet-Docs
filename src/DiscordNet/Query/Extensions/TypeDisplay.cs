using Discord;
using DiscordNet.Github;
using DiscordNet.Handlers;
using DiscordNet.Query.Results;
using DiscordNet.Query.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace DiscordNet.Query
{
    public partial class ResultDisplay
    {
        private async Task<EmbedBuilder> ShowTypesAsync(EmbedBuilder eb, EmbedAuthorBuilder eab, IEnumerable<TypeInfoWrapper> list)
        {
            TypeInfoWrapper first = list.First();
            DocsHttpResult result;
            string pageUrl = SanitizeDocsUrl($"{first.TypeInfo.Namespace}.{first.TypeInfo.Name}");
            try
            {
                result = await GetWebDocsAsync($"{DocsUrlHandler.DocsBaseUrl}api/{pageUrl}.html", first);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                result = new DocsHttpResult($"{DocsUrlHandler.DocsBaseUrl}api/{pageUrl}.html");
            }
            eab.Name = $"{(first.TypeInfo.IsInterface ? "Interface" : (first.TypeInfo.IsEnum ? "Enum" : "Type"))}: {first.TypeInfo.Namespace}.{first.DisplayName}";
            eab.Url = result.Url;//$"{DocsUrlHandler.DocsBaseUrl}api/{first.Namespace}.{first.Name}.html";
            eb.AddField((x) =>
            {
                x.IsInline = true;
                x.Name = "Docs:";
                x.Value = FormatDocsUrl(eab.Url);
            });
            var githubUrl = await GithubRest.GetTypeUrlAsync(first);
            if (githubUrl != null)
                eb.AddField((x) =>
                {
                    x.IsInline = true;
                    x.Name = "Source:";
                    x.Value = FormatGithubUrl(githubUrl);
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
            CacheBag cb = _cache.GetCacheBag(first);
            if (cb.Methods.Count != 0)
            {
                int i = 1;
                var methods = cb.Methods.RandomShuffle().Take(3);
                eb.AddField((x) =>
                {
                    x.IsInline = true;
                    x.Name = $"Some methods ({methods.Count()}/{cb.Methods.Count}):";
                    x.Value = String.Join("\n", methods.Select(y => $"``{i++}-``{(IsInherited(new MethodInfoWrapper(first, y)) ? " (i)" : "")} {y.Name}(...)"));
                });
            }
            if (cb.Properties.Count != 0)
            {
                int i = 1;
                var properties = cb.Properties.RandomShuffle().Take(3);
                eb.AddField((x) =>
                {
                    x.IsInline = true;
                    x.Name = $"Some properties ({properties.Count()}/{cb.Properties.Count}):";
                    x.Value = String.Join("\n", properties.Select(y => $"``{i++}-``{(IsInherited(new PropertyInfoWrapper(first, y)) ? " (i)" : "")} {y.Name}"));
                });
            }
            if (first.TypeInfo.IsEnum)
            {
                var enumValues = first.TypeInfo.GetEnumNames();
                int i = 1;
                var fields = enumValues.RandomShuffle().Take(3);
                eb.AddField((x) =>
                {
                    x.IsInline = true;
                    x.Name = $"Some fields ({fields.Count()}/{enumValues.Length}):";
                    x.Value = String.Join("\n", fields.Select(y => $"``{i++}-`` {y}"));
                });
            }
            return eb;
        }
    }
}
