using DiscordNet.Docs;
using System.Threading.Tasks;

namespace DiscordNet.Handlers
{
    public class DocsHandler
    {
        public async Task<string> Run(string text)
        {
            var interpreterResult = new TextInterpreter(text).Run();
            var searchResult = new Search(interpreterResult).Run();

            if (searchResult.Count != 0)
                return await new ResultDisplay(searchResult).Run();
            else
                return $"No results found for ``{text}``.";
        }
    }
}
