using Discord.WebSocket;
using DiscordNet.Handlers;
using System;
using System.Threading.Tasks;

namespace DiscordNet.Controllers
{
    public class MainHandler
    {
        public DiscordSocketClient Client;
        public IServiceProvider Services;

        public CommandHandler CommandHandler { get; private set; }
        public QueryHandler QueryHandler { get; private set; }

        public readonly string Prefix = "<@274366085011079169> ";

        public MainHandler(DiscordSocketClient client, IServiceProvider services)
        {
            Client = client;
            Services = services;
            CommandHandler = new CommandHandler();
            QueryHandler = new QueryHandler();
        }

        public async Task InitializeEarlyAsync()
        {
            await CommandHandler.InitializeAsync(this, Services);
            QueryHandler.Initialize();
        }

        public MainHandler() => new MainHandler();
    }
}
