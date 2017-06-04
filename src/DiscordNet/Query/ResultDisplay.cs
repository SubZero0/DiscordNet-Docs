using Discord;
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
        private SearchResult<object> _result;
        private Cache _cache;
        public ResultDisplay(SearchResult<object> result, Cache cache)
        {
            _result = result;
            _cache = cache;
        }

        public async Task<EmbedBuilder> RunAsync()
        {
            var list = _result.List.GroupBy(x => GetPath(x, false));
            if (list.Count() == 1)
                return await ShowAsync(list.First());
            else
                return await ShowMultipleAsync(list);
        }

        private async Task<EmbedBuilder> ShowAsync(IEnumerable<object> o)
        {
            var first = o.First();
            EmbedAuthorBuilder eab = new EmbedAuthorBuilder();
            eab.IconUrl = "http://i.imgur.com/XW4RU5e.png";
            EmbedBuilder eb = new EmbedBuilder().WithAuthor(eab);
            if (first is TypeInfoWrapper)
                eb = await ShowTypesAsync(eb, eab, o.Select(x => (TypeInfoWrapper)x));
            else if (first is MethodInfoWrapper)
                eb = await ShowMethodsAsync(eb, eab, o.Select(x => (MethodInfoWrapper)x));
            else if(first is PropertyInfoWrapper)
                eb = await ShowPropertiesAsync(eb, eab, o.Select(x => (PropertyInfoWrapper)x));
            else if (first is EventInfoWrapper)
                eb = await ShowEventsAsync(eb, eab, o.Select(x => (EventInfoWrapper)x));
            return eb;
        }

        private async Task<EmbedBuilder> ShowMultipleAsync(IEnumerable<IEnumerable<object>> obj)
        {
            EmbedBuilder eb = new EmbedBuilder();
            var singleList = obj.Select(x => x.First());
            var same = singleList.GroupBy(x => GetSimplePath(x));
            if (same.Count() == 1)
            {
                eb = await ShowAsync(obj.First());
                eb.Author.Name = $"(Most likely) {eb.Author.Name}";
                var list = singleList.Skip(1).RandomShuffle().Take(6);
                int max = (int)Math.Ceiling(list.Count() / 3.0);
                for (int i = 0; i < max; i++)
                    eb.AddField(x =>
                    {
                        x.Name = (i == 0 ? $"Also found in ({list.Count()}/{singleList.Count() - 1}):" : "​");
                        x.Value = string.Join("\n", GetNamespaces(list.Skip(3*i).Take(3)));
                        x.IsInline = true;
                    });
            }
            else
            {
                if (singleList.Count() > 10)
                {
                    eb.Title = $"Too many results, try filtering your search. Some results (10/{obj.Count()}):";
                    eb.Description = string.Join("\n", GetPaths(singleList.Take(10)));
                }
                else
                {
                    eb.Title = "Did you mean:";
                    eb.Description = string.Join("\n", GetPaths(singleList));
                }
            }
            eb.Footer = new EmbedFooterBuilder().WithText("Type help to see keywords to filter your query.");
            return eb;
        }
    }
}
