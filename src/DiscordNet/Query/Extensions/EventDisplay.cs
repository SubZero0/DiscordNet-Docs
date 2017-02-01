using Discord;
using DiscordNet.Query.Results;
using DiscordNet.Query.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace DiscordNet.Query.Extensions
{
    public static class EventDisplay
    {
        public static async Task<EmbedBuilder> ShowEventsAsync(EmbedBuilder eb, EmbedAuthorBuilder eab, IEnumerable<EventInfoWrapper> list)
        {
            EventInfoWrapper first = list.First();
            DocsHttpResult result;
            string pageUrl = $"{first.Parent.TypeInfo.Namespace}.{first.Parent.TypeInfo.Name}".SanitizeDocsUrl();
            try
            {
                result = await BaseDisplay.GetWebDocsAsync($"https://discord.foxbot.me/docs/api/{pageUrl}.html", first);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                result = new DocsHttpResult($"https://discord.foxbot.me/docs/api/{pageUrl}.html{EventToDocs(first)}");
            }
            eab.Name = $"Event: {first.Parent.TypeInfo.Namespace}.{first.Parent.DisplayName}.{first.Event.Name}";
            eab.Url = result.Url; //$"https://discord.foxbot.me/docs/api/{first.DeclaringType.Namespace}.{first.DeclaringType.Name}.html{EventToDocs(first)}";
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
            return eb;
        }

        private static string EventToDocs(EventInfoWrapper ei)
        {
            return $"#{ei.Parent.TypeInfo.Namespace.Replace('.', '_')}_{ei.Parent.TypeInfo.Name}_{ei.Event.Name}";
        }
    }
}
