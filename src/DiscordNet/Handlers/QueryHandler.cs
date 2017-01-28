using Discord;
using DiscordNet.Query;
using System;
using System.Threading.Tasks;

namespace DiscordNet.Handlers
{
    public class QueryHandler
    {
        public async Task<Tuple<string, EmbedBuilder>> Run(string text)
        {
            var interpreterResult = new TextInterpreter(text).Run();
            var searchResult = new Search(interpreterResult).Run();

            if (searchResult.Count != 0)
                return Tuple.Create("", await new ResultDisplay(searchResult).Run());
            else
                return Tuple.Create($"No results found for ``{text}``.", (EmbedBuilder)null);
        }
    }
}
