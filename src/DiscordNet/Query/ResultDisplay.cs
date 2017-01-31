using Discord;
using DiscordNet.Query.Results;
using DiscordNet.Query.Wrappers;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DiscordNet.Query.Extensions;

namespace DiscordNet.Query
{
    public class ResultDisplay
    {
        private SearchResult<object> _result;
        protected internal Cache _cache;
        public ResultDisplay(SearchResult<object> result, Cache cache)
        {
            _result = result;
            _cache = cache;
        }

        public async Task<EmbedBuilder> RunAsync()
        {
            var list = _result.List.GroupBy(x => BaseDisplay.GetPath(x, false));
            if (list.Count() == 1)
                return await ShowAsync(list.ElementAt(0));
            else
                return ShowMultiple(list.Select(x => x.First()));
        }

        private async Task<EmbedBuilder> ShowAsync(IEnumerable<object> o)
        {
            var first = o.First();
            EmbedAuthorBuilder eab = new EmbedAuthorBuilder();
            eab.IconUrl = "http://i.imgur.com/XW4RU5e.png";
            EmbedBuilder eb = new EmbedBuilder().WithAuthor(eab);
            if (first is TypeInfo)
                eb = await this.ShowTypesAsync(eb, eab, o.Select(x => (TypeInfo)x));
            else if (first is MethodInfoWrapper)
                eb = await MethodDisplay.ShowMethodsAsync(eb, eab, o.Select(x => (MethodInfoWrapper)x));
            else if(first is PropertyInfoWrapper)
                eb = await PropertyDisplay.ShowPropertiesAsync(eb, eab, o.Select(x => (PropertyInfoWrapper)x));
            else if (first is EventInfo)
                eb = await EventDisplay.ShowEventsAsync(eb, eab, o.Select(x => (EventInfo)x));
            return eb;
        }

        private EmbedBuilder ShowMultiple(IEnumerable<object> obj)
        {
            EmbedBuilder eb = new EmbedBuilder();
            if (obj.Count() > 10)
            {
                eb.Title = "Too many results, try filtering your search. Some results:";
                eb.Description = string.Join("\n", BaseDisplay.GetPaths(obj.Take(10)));
            }
            else
            {
                eb.Title = "Did you mean:";
                eb.Description = string.Join("\n", BaseDisplay.GetPaths(obj));
                eb.Footer = new EmbedFooterBuilder().WithText("Type help to see keywords to filter your query.");
            }
            return eb;
        }
    }
}
