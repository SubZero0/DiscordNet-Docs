using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DiscordNet.Query
{
    public partial class ResultDisplay
    {
        private async Task<EmbedBuilder> ShowPropertiesAsync(EmbedBuilder eb, EmbedAuthorBuilder eab, IEnumerable<PropertyInfoWrapper> list)
        {
            PropertyInfoWrapper first = list.First();
            DocsHttpResult result;
            string pageUrl = SanitizeDocsUrl($"{first.Parent.TypeInfo.Namespace}.{first.Parent.TypeInfo.Name}");
            try
            {
                result = await GetWebDocsAsync($"{DocsUrlHandler.DocsBaseUrl}api/{pageUrl}.html", first);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                result = new DocsHttpResult($"{DocsUrlHandler.DocsBaseUrl}api/{pageUrl}.html{PropertyToDocs(first)}");
            }
            eab.Name = $"Property: {first.Parent.TypeInfo.Namespace}.{first.Parent.DisplayName}.{first.Property.Name} {(IsInherited(first) ? "(i)" : "")}";
            eab.Url = result.Url;//$"{DocsUrlHandler.DocsBaseUrl}api/{first.Parent.Namespace}.{first.Parent.Name}.html{PropertyToDocs(first)}";
            eb.AddField((x) =>
            {
                x.IsInline = true;
                x.Name = "Docs:";
                x.Value = FormatDocsUrl(eab.Url);
            });
            var githubUrl = await _githubRest.GetPropertyUrlAsync(first);
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
            eb.AddField((x) =>
            {
                x.IsInline = false;
                x.Name = "Return type:";
                x.Value = Utils.BuildType(first.Property.PropertyType);
            });
            /*eb.AddField((x) =>
            {
                x.IsInline = true;
                x.Name = "Get & Set:";
                x.Value = $"Can write: {first.Property.CanWrite}\nCan read: {first.Property.CanRead}";
            });*/
            return eb;
        }

        private string PropertyToDocs(PropertyInfoWrapper pi)
            => IsInherited(pi) ? "" : $"#{pi.Parent.TypeInfo.Namespace.Replace('.', '_')}_{pi.Parent.TypeInfo.Name}_{pi.Property.Name}";
    }
}
