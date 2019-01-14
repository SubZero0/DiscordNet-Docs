using System;

namespace DiscordNet.Handlers
{
    public class DocsUrlHandler
    {
        public static string DocsBaseUrl { get; set; } = "https://docs.stillu.cc/";

        private string[] _docsUrls =
        {
            "https://docs.stillu.cc/",
            "https://discord.foxbot.me/latest/",
            "http://discord.devpaulo.com.br/"
        };

        public bool CheckAvailability()
        {
            throw new NotImplementedException();
        }
    }
}
