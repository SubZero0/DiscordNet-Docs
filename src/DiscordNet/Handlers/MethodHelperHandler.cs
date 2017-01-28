using Discord;
using DiscordNet.MethodHelper;
using System;
using System.Threading.Tasks;

namespace DiscordNet.Handlers
{
    public class MethodHelperHandler
    {
        public Tuple<string, EmbedBuilder> Run(string text)
        {
            var interpreterResult = new TextInterpreter(text).Run();
            var searchResult = new Search(interpreterResult).Run();

            if (searchResult.Count != 0)
                return new ResultDisplay(searchResult).Run();
            else
                return Tuple.Create($"No results found for ``{text}``.", (EmbedBuilder)null);
        }
    }
}
