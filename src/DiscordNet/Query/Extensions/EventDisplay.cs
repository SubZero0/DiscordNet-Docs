using Discord;
using DiscordNet.Query.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace DiscordNet.Query.Extensions
{
    public static class EventDisplay
    {
        public static async Task<EmbedBuilder> ShowEventsAsync(EmbedBuilder eb, EmbedAuthorBuilder eab, IEnumerable<EventInfo> list)
        {
            EventInfo first = list.First();
            DocsHttpResult result;
            try
            {
                result = await BaseDisplay.GetWebDocsAsync($"https://discord.foxbot.me/docs/api/{first.DeclaringType.Namespace}.{first.DeclaringType.Name}.html", first);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                result = new DocsHttpResult($"https://discord.foxbot.me/docs/api/{first.DeclaringType.Namespace}.{first.DeclaringType.Name}.html{EventToDocs(first)}");
            }
            eab.Name = $"Event: {first.DeclaringType.Namespace}.{first.DeclaringType.Name}.{first.Name}";
            eab.Url = $"https://discord.foxbot.me/docs/api/{first.DeclaringType.Namespace}.{first.DeclaringType.Name}.html{EventToDocs(first)}"; //or result.Url
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

        private static string EventToDocs(EventInfo ei)
        {
            return $"#{ei.DeclaringType.Namespace.Replace('.', '_')}_{ei.DeclaringType.Name}_{ei.Name}";
        }
    }
}
