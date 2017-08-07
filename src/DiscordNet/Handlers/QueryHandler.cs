using Discord;
using DiscordNet.Query;
using DiscordNet.Query.Results;
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

        public async Task<(string, EmbedBuilder)> RunAsync(string text)
        {
            var interpreterResult = new TextInterpreter(text).Run();
            if (!interpreterResult.IsSuccess)
                return ($"{interpreterResult.Error}", null);

            EmbedBuilder result;
            if (interpreterResult.Search == SearchType.JUST_NAMESPACE)
                result = await SearchAsync(interpreterResult, SearchType.NONE) ?? await SearchAsync(interpreterResult, SearchType.JUST_NAMESPACE) ?? await SearchAsync(interpreterResult, SearchType.JUST_TEXT) ?? await SearchAsync(interpreterResult, SearchType.ALL);
            else
                result = await SearchAsync(interpreterResult, SearchType.NONE) ?? await SearchAsync(interpreterResult, SearchType.JUST_TEXT) ?? await SearchAsync(interpreterResult, SearchType.JUST_NAMESPACE) ?? await SearchAsync(interpreterResult, SearchType.ALL);

            return result == null ? ($"No results found for ``{text}``.", null) : ("", result);
        }

        private async Task<EmbedBuilder> SearchAsync(InterpreterResult interpreterResult, SearchType type)
        {
            interpreterResult.Search = type;
            var searchResult = new Search(interpreterResult, Cache).Run();
            if (searchResult.Count != 0)
                return await new ResultDisplay(searchResult, Cache, interpreterResult.IsList).RunAsync();
            return null;
        }

        public bool IsReady()
        {
            return Cache.IsReady();
        }
    }
}
