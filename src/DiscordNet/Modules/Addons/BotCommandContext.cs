using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace DiscordNet
{
    public class BotCommandContext : ICommandContext
    {
        public DiscordSocketClient Client { get; }
        public SocketGuild Guild { get; }
        public MainController MainController { get; }
        public IMessageChannel Channel { get; }
        public IUser User { get; }
        public IUserMessage Message { get; }

        public bool IsPrivate => Channel is IPrivateChannel;

        public BotCommandContext(DiscordSocketClient client, MainController controller, IUserMessage msg)
        {
            Client = client;
            Guild = (msg.Channel as SocketGuildChannel)?.Guild;
            Channel = msg.Channel;
            User = msg.Author;
            Message = msg;
            MainController = controller;
        }

        IDiscordClient ICommandContext.Client => Client;
        IGuild ICommandContext.Guild => Guild;
    }
}
