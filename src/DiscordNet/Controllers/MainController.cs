using Discord.WebSocket;
using DiscordNet.Github;
using System;
using System.Threading.Tasks;

namespace DiscordNet
{
    public class MainController
    {
        public DiscordSocketClient Client;
        public IServiceProvider Services;

        public CommandHandler CommandHandler { get; private set; }
        public QueryHandler QueryHandler { get; private set; }

        public readonly string Prefix = "<@274366085011079169> ";

        public MainController(DiscordSocketClient client, IServiceProvider services)
        {
            Client = client;
            Services = services;
            CommandHandler = new CommandHandler();
            QueryHandler = new QueryHandler((GithubRest)services.GetService(typeof(GithubRest)));
        }

        public async Task InitializeEarlyAsync()
        {
            await CommandHandler.InitializeAsync(this, Services);
            QueryHandler.Initialize();
        }
    }
}
