using Discord;
using DiscordNet.Github;
using DiscordNet.Query;
using System.Threading.Tasks;

namespace DiscordNet
{
    public class QueryHandler
    {
        public Cache Cache { get; }
        public GithubRest GithubRest { get; }

        public QueryHandler(GithubRest githubRest)
        {
            Cache = new Cache();
            GithubRest = githubRest;
        }

        public void Initialize()
            => Cache.Initialize();

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
                return await new ResultDisplay(searchResult, Cache, interpreterResult.IsList, GithubRest).RunAsync();
            return null;
        }

        public bool IsReady()
            => Cache.IsReady();
    }
}
