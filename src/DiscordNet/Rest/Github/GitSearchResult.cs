namespace DiscordNet.Github
{
    public class GitSearchResult
    {
        public string Name { get; protected internal set; }
        public string HtmlUrl { get; protected internal set; }

        public override string ToString()
        {
            return $"{Name}: {HtmlUrl}";
        }
    }
}
