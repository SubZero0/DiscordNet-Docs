using System;

namespace DiscordNet.Handlers
{
    public class DocsUrlHandler
    {
        public static string DocsBaseUrl { get; set; } = "https://discord.foxbot.me/docs/";

        private string[] _docsUrls =
        {
            "https://discord.foxbot.me/docs/",
            "http://discord.devpaulo.com.br/"
        };

        public bool CheckAvailability()
        {
            throw new NotImplementedException();
        }
    }
}
