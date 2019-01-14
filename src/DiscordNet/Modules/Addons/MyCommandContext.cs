using Discord;
using Discord.Commands;
using DiscordNet.Controllers;

namespace DiscordNet.Modules.Addons
{
    public class MyCommandContext : ICommandContext
    {
        public IDiscordClient Client { get; }
        public IGuild Guild { get; }
        public MainController MainController { get; }
        public IMessageChannel Channel { get; }
        public IUser User { get; }
        public IUserMessage Message { get; }

        public bool IsPrivate => Channel is IPrivateChannel;

        public MyCommandContext(IDiscordClient client, MainController controller, IUserMessage msg)
        {
            Client = client;
            Guild = (msg.Channel as IGuildChannel)?.Guild;
            Channel = msg.Channel;
            User = msg.Author;
            Message = msg;
            MainController = controller;
        }
    }
}
