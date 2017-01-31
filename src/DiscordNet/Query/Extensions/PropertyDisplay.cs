using Discord;
using DiscordNet.Query.Results;
using DiscordNet.Query.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DiscordNet.Query.Extensions
{
    public static class PropertyDisplay
    {
        public static async Task<EmbedBuilder> ShowPropertiesAsync(EmbedBuilder eb, EmbedAuthorBuilder eab, IEnumerable<PropertyInfoWrapper> list)
        {
            PropertyInfoWrapper first = list.First();
            DocsHttpResult result;
            try
            {
                result = await BaseDisplay.GetWebDocsAsync($"https://discord.foxbot.me/docs/api/{first.Parent.Namespace}.{first.Parent.Name}.html", first);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                result = new DocsHttpResult($"https://discord.foxbot.me/docs/api/{first.Parent.Namespace}.{first.Parent.Name}.html{PropertyToDocs(first)}");
            }
            eab.Name = $"Property: {first.Parent.Namespace}.{first.Parent.Name}.{first.Property.Name} {(BaseDisplay.IsInherited(first) ? "(i)" : "")}";
            eab.Url = $"https://discord.foxbot.me/docs/api/{first.Parent.Namespace}.{first.Parent.Name}.html{PropertyToDocs(first)}"; //or result.Url
            eb.AddField((x) =>
            {
                x.IsInline = false;
                x.Name = "Docs:";
                x.Value = eab.Url;
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
            /*eb.AddField((x) =>
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
            });*/
            return eb;
        }

        private static string PropertyToDocs(PropertyInfoWrapper pi)
        {
            if (BaseDisplay.IsInherited(pi))
                return "";
            return $"#{pi.Parent.Namespace.Replace('.', '_')}_{pi.Parent.Name}_{pi.Property.Name}";
        }
    }
}
