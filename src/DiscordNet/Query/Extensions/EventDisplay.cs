using Discord;
using DiscordNet.Github;
using DiscordNet.Query.Results;
using DiscordNet.Query.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DiscordNet.Query
{
    public partial class ResultDisplay
    {
        private async Task<EmbedBuilder> ShowEventsAsync(EmbedBuilder eb, EmbedAuthorBuilder eab, IEnumerable<EventInfoWrapper> list)
        {
            EventInfoWrapper first = list.First();
            DocsHttpResult result;
            string pageUrl = SanitizeDocsUrl($"{first.Parent.TypeInfo.Namespace}.{first.Parent.TypeInfo.Name}");
            try
            {
                result = await GetWebDocsAsync($"https://discord.foxbot.me/docs/api/{pageUrl}.html", first);
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
                x.IsInline = true;
                x.Name = "Docs:";
                x.Value = FormatDocsUrl(eab.Url);
            });
            var githubUrl = await GithubRest.GetEventUrlAsync(first);
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
                x.Name = "Arguments:";
                x.Value = BuildEvent(first);
            });
            return eb;
        }

        private string EventToDocs(EventInfoWrapper ei)
        {
            return $"#{ei.Parent.TypeInfo.Namespace.Replace('.', '_')}_{ei.Parent.TypeInfo.Name}_{ei.Event.Name}";
        }

        private string BuildEvent(EventInfoWrapper ev)
        {
            IEnumerable<Type> par = ev.Event.EventHandlerType.GenericTypeArguments;
            par = par.Take(par.Count() - 1);
            return $"({String.Join(", ", par.Select(x => $"{Utils.BuildType(x)}"))})";
        }
    }
}
