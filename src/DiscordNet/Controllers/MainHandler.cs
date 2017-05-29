using Discord.WebSocket;
using DiscordNet.Handlers;
using System;
using System.Threading.Tasks;

namespace DiscordNet.Controllers
{
    public class MainHandler
    {
        public DiscordSocketClient Client;

        public CommandHandler CommandHandler { get; private set; }
        public QueryHandler QueryHandler { get; private set; }

        public readonly string Prefix = "<@274366085011079169> ";

        public MainHandler(DiscordSocketClient Discord)
        {
            Client = Discord;
            CommandHandler = new CommandHandler();
            QueryHandler = new QueryHandler();
        }

        public async Task InitializeEarlyAsync(IServiceProvider map)
        {
            await CommandHandler.InitializeAsync(this, map);
            QueryHandler.Initialize();
        }
    }
}
