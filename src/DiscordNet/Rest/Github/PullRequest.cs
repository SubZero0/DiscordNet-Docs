using Newtonsoft.Json.Linq;

namespace DiscordNet.Github
{
    public class PullRequest
    {
        public string Repository { get; }
        public string Ref { get; }

        public PullRequest(JObject response)
        {
            Repository = (string)response["head"]["repo"]["full_name"];
            Ref = (string)response["head"]["ref"];
        }
    }
}
