using Discord;
using Discord.Addons.Paginator;
using DiscordNet.EmbedExtension;
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
        private bool _isList;
        public ResultDisplay(SearchResult<object> result, Cache cache, bool isList)
        {
            _result = result;
            _cache = cache;
            _isList = isList;
        }

        public async Task<EmbedBuilder> RunAsync()
        {
            var list = _result.List.GroupBy(x => GetPath(x, false));
            if (_isList)
                return ShowList(list);
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
                        x.Value = String.Join("\n", list.Skip(3 * i).Take(3).Select(y => GetParent(y)));
                        x.IsInline = true;
                    });
            }
            else
            {
                /*if (singleList.Count() > 10)
                {
                    eb.Title = $"Too many results, try filtering your search. Some results (10/{obj.Count()}):";
                    eb.Description = string.Join("\n", GetPaths(singleList.Take(10)));
                }
                else
                {
                    eb.Title = "Did you mean:";
                    eb.Description = string.Join("\n", GetPaths(singleList));
                }*/
                eb = await ShowAsync(obj.First());
                eb.Author.Name = $"(First) {eb.Author.Name}";
                var list = singleList.Skip(1).RandomShuffle().Take(3);
                eb.AddField(x =>
                {
                    x.Name = $"Other results ({list.Count()}/{singleList.Count() - 1}):";
                    x.Value = string.Join("\n", GetPaths(list));
                    x.IsInline = false;
                });
            }
            eb.Footer = new EmbedFooterBuilder().WithText("Type help to see keywords to filter your query.");
            return eb;
        }

        private EmbedBuilder ShowList(IEnumerable<IEnumerable<object>> obj)
        {
            PaginatorBuilder eb = new PaginatorBuilder();
            var singleList = obj.Select(x => x.First());
            int size = 10;
            int pages = (int)Math.Ceiling(singleList.Count() / (float)size);
            eb.Pages = ToPages(singleList, pages, size);
            return eb;
        }

        private IEnumerable<string> ToPages(IEnumerable<object> list, int pages, int size)
        {
            for (int i = 0; i < pages; i++)
                yield return String.Join("\n", GetPaths(list.Skip(i * size).Take(size)));
        }
    }
}
