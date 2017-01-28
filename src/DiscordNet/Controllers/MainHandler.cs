using Discord.Commands;
using Discord.WebSocket;
using DiscordNet.Handlers;
using System.Threading.Tasks;

namespace DiscordNet.Controllers
{
    public class MainHandler
    {
        public DiscordSocketClient Client;

        public CommandHandler CommandHandler { get; private set; }
        public DocsHandler DocsHandler { get; private set; }
        public MethodHelperHandler MethodHelperHandler { get; private set; }

        public readonly string Prefix = "dnet ";

        public MainHandler(DiscordSocketClient Discord)
        {
            Client = Discord;
            CommandHandler = new CommandHandler();
            DocsHandler = new DocsHandler();
            MethodHelperHandler = new MethodHelperHandler();
        }

        public async Task InitializeEarlyAsync(IDependencyMap map)
        {
            await CommandHandler.InitializeAsync(this, map);
        }
    }
}
