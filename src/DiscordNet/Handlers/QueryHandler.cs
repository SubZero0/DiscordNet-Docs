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
            var searchResult = new Search(interpreterResult, Cache).Run();

            if (searchResult.Count != 0)
                return Tuple.Create("", await new ResultDisplay(searchResult, Cache).RunAsync());
            else
                return Tuple.Create($"No results found for ``{text}``.", (EmbedBuilder)null);
        }

        public bool IsReady()
        {
            return Cache.IsReady();
        }
    }
}
