using System;

namespace DiscordNet.Handlers
{
    public class DocsUrlHandler
    {
        public static string DocsBaseUrl { get; set; } = "https://discord.foxbot.me/latest/";

        private string[] _docsUrls =
        {
            "https://discord.foxbot.me/latest/",
            "http://discord.devpaulo.com.br/"
        };

        public bool CheckAvailability()
        {
            throw new NotImplementedException();
        }
    }
}
