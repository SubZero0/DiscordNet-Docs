using Discord;
using DiscordNet.Query;
using System;
using System.Threading.Tasks;

namespace DiscordNet.Handlers
{
    public class QueryHandler
    {
        public Cache Cache { get; private set; }
        public QueryHandler()
        {
            Cache = new Cache();
        }

        public void Initialize()
        {
            Cache.Initialize();
        }

        public async Task<Tuple<string, EmbedBuilder>> RunAsync(string text)
        {
            var interpreterResult = new TextInterpreter(text).Run();
            if (!interpreterResult.IsSuccess)
                return Tuple.Create($"{interpreterResult.Error}", (EmbedBuilder)null);
            var searchResult = new Search(interpreterResult, Cache).Run();

            if (searchResult.Count != 0)
                return Tuple.Create("", await new ResultDisplay(searchResult, Cache).RunAsync());
            else
            {
                if (interpreterResult.Search != SearchType.JUST_TEXT && interpreterResult.Search != SearchType.ALL)
                {
                    if (interpreterResult.Search == SearchType.JUST_NAMESPACE)
                        interpreterResult.Search = SearchType.ALL;
                    else
                        interpreterResult.Search = SearchType.JUST_TEXT;
                    searchResult = new Search(interpreterResult, Cache).Run();
                    if (searchResult.Count != 0)
                        return Tuple.Create("", await new ResultDisplay(searchResult, Cache).RunAsync());
                }
                return Tuple.Create($"No results found for ``{text}``.", (EmbedBuilder)null);
            }
        }

        public bool IsReady()
        {
            return Cache.IsReady();
        }
    }
}
