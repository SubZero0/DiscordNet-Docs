using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordNet.Controllers;
using DiscordNet.Modules.Addons;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace DiscordNet.Handlers
{
    public class CommandHandler
    {
        private CommandService commands;
        private DiscordSocketClient client;
        private IDependencyMap map;
        private MainHandler MainHandler;

        public async Task InitializeAsync(MainHandler MainHandler, IDependencyMap _map)
        {
            this.MainHandler = MainHandler;
            client = _map.Get<DiscordSocketClient>();
            commands = new CommandService();
            _map.Add(commands);
            map = _map;

            await commands.AddModulesAsync(Assembly.GetEntryAssembly());

            client.MessageReceived += HandleCommand;
        }

        public Task Close()
        {
            client.MessageReceived -= HandleCommand;
            return Task.CompletedTask;
        }

        public Task HandleCommand(SocketMessage parameterMessage)
        {
            var msg = parameterMessage as SocketUserMessage;
            if (msg == null) return Task.CompletedTask;
            if (msg.Channel.Name != "dotnet_discord-net" && msg.Channel.Name != "testing") return Task.CompletedTask;
            int argPos = 0;
            if (!(msg.HasMentionPrefix(client.CurrentUser, ref argPos) || msg.HasStringPrefix(MainHandler.Prefix, ref argPos))) return Task.CompletedTask;
            var _ = HandleCommandAsync(msg, argPos);
            return Task.CompletedTask;
        }

        public async Task HandleCommandAsync(SocketUserMessage msg, int argPos)
        {
            var context = new MyCommandContext(client, MainHandler, msg);
            var result = await commands.ExecuteAsync(context, argPos, map);
            if (!result.IsSuccess && result.Error != CommandError.UnknownCommand)
                await msg.Channel.SendMessageAsync(result.ErrorReason);
        }

        public async Task<EmbedBuilder> HelpEmbedBuilder(ICommandContext context, string command = null)
        {
            EmbedBuilder eb = new EmbedBuilder();
            eb.Author = new EmbedAuthorBuilder().WithName("Help:").WithIconUrl("http://i.imgur.com/VzDRjUn.png");
            eb.Description = "";
            if (command == null)
            {
                foreach (ModuleInfo mi in commands.Modules.OrderBy(x => x.Name))
                    if (!mi.IsSubmodule)
                        if (mi.Name != "Help")
                        {
                            bool ok = true;
                            foreach (PreconditionAttribute precondition in mi.Preconditions)
                                if (!(await precondition.CheckPermissions(context, null, map)).IsSuccess)
                                {
                                    ok = false;
                                    break;
                                }
                            if (ok)
                            {
                                var cmds = mi.Commands.ToList<object>();
                                cmds.AddRange(mi.Submodules);
                                for (int i = cmds.Count - 1; i >= 0; i--)
                                {
                                    object o = cmds[i];
                                    foreach (PreconditionAttribute precondition in ((o as CommandInfo)?.Preconditions ?? (o as ModuleInfo)?.Preconditions))
                                        if (!(await precondition.CheckPermissions(context, o as CommandInfo, map)).IsSuccess)
                                            cmds.Remove(o);
                                }
                                if (cmds.Count != 0)
                                {
                                    var list = cmds.OrderBy(x => ((x as CommandInfo)?.Name ?? (x as ModuleInfo)?.Name)).Select(x => $"{MainHandler.Prefix}{((x as CommandInfo)?.Name ?? (x as ModuleInfo)?.Name)}");
                                    eb.Description += $"**{mi.Name}:** {String.Join(", ", list)}\n";
                                }
                            }
                        }
            }
            else
            {
                SearchResult sr = commands.Search(context, command);
                if (sr.IsSuccess)
                {
                    Nullable<CommandMatch> cmd = null;
                    if (sr.Commands.Count == 1)
                        cmd = sr.Commands.First();
                    else
                    {
                        int lastIndex;
                        var find = sr.Commands.Where(x => x.Command.Aliases.First().Equals(command, StringComparison.OrdinalIgnoreCase));
                        if (find.Count() != 0)
                            cmd = find.First();
                        while (cmd == null && (lastIndex = command.LastIndexOf(' ')) != -1) //TODO: Maybe remove and say command not found?
                        {
                            find = sr.Commands.Where(x => x.Command.Aliases.First().Equals(command.Substring(0, lastIndex), StringComparison.OrdinalIgnoreCase));
                            if (find.Count() != 0)
                                cmd = find.First();
                            command = command.Substring(0, lastIndex);
                        }
                    }
                    if (cmd != null && (await cmd.Value.CheckPreconditionsAsync(context, map)).IsSuccess)
                    {
                        eb.Author.Name = $"Help: {cmd.Value.Command.Aliases.First()}";
                        eb.Description = $"Usage: {MainHandler.Prefix}{cmd.Value.Command.Aliases.First()}";
                        if (cmd.Value.Command.Parameters.Count != 0)
                            eb.Description += $" [{String.Join("] [", cmd.Value.Command.Parameters.Select(x => x.Name))}]";
                        if (!String.IsNullOrEmpty(cmd.Value.Command.Summary))
                            eb.Description += $"\nSummary: {cmd.Value.Command.Summary}";
                        if (!String.IsNullOrEmpty(cmd.Value.Command.Remarks))
                            eb.Description += $"\nRemarks: {cmd.Value.Command.Remarks}";
                        if (cmd.Value.Command.Aliases.Count != 1)
                            eb.Description += $"\nAliases: {String.Join(", ", cmd.Value.Command.Aliases.Where(x => x != cmd.Value.Command.Aliases.First()))}";
                    }
                    else
                        eb.Description = $"Command '{command}' not found.";
                }
                else
                    eb.Description = $"Command '{command}' not found.";
            }
            return eb;
        }
    }
}
