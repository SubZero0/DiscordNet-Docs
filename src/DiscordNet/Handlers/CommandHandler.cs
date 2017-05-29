using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordNet.Controllers;
using DiscordNet.Modules.Addons;
using System;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DiscordNet.Handlers
{
    public class CommandHandler
    {
        private CommandService commands;
        private DiscordSocketClient client;
        private IServiceProvider services;
        private MainHandler MainHandler;

        public async Task InitializeAsync(MainHandler MainHandler, IServiceProvider services)
        {
            this.MainHandler = MainHandler;
            client = (DiscordSocketClient)services.GetService(typeof(DiscordSocketClient));
            commands = new CommandService();
            this.services = services;

            await commands.AddModulesAsync(Assembly.GetEntryAssembly());
            commands.Log += Log;

            client.MessageReceived += HandleCommand;
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.Exception.ToString());
            return Task.CompletedTask;
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
            if (msg.Author.IsBot) return Task.CompletedTask;
            if (msg.Channel.Name != "dotnet_discord-net" && msg.Channel.Name != "testing" && msg.Channel.Name != "playground") return Task.CompletedTask;
            int argPos = 0;
            if (!(msg.HasMentionPrefix(client.CurrentUser, ref argPos) /*|| msg.HasStringPrefix(MainHandler.Prefix, ref argPos)*/)) return Task.CompletedTask;
            var _ = HandleCommandAsync(msg, argPos);
            return Task.CompletedTask;
        }

        public async Task HandleCommandAsync(SocketUserMessage msg, int argPos)
        {
            var context = new MyCommandContext(client, MainHandler, msg);
            var result = await commands.ExecuteAsync(context, argPos, services);
            if (!result.IsSuccess && result.Error != CommandError.UnknownCommand)
                await msg.Channel.SendMessageAsync(result.ErrorReason);
            else if (!result.IsSuccess)
            {
                string query = msg.Content.Substring(argPos);
                if (!MainHandler.QueryHandler.IsReady())
                {
                    await msg.Channel.SendMessageAsync("Loading cache..."); //TODO: Change message
                }
                else
                {
                    try
                    {
                        var tuple = await MainHandler.QueryHandler.RunAsync(query);
                        await msg.Channel.SendMessageAsync(tuple.Item1, false, tuple.Item2);
                    }
                    catch (Exception e)
                    {
                        await msg.Channel.SendMessageAsync("Uh-oh... I think some pipes have broken...");
                        Console.WriteLine(e.ToString());
                    }
                }
            }
        }

        public async Task<EmbedBuilder> HelpEmbedBuilderAsync(ICommandContext context, string command = null)
        {
            EmbedBuilder eb = new EmbedBuilder();
            eb.Author = new EmbedAuthorBuilder().WithName("Help:").WithIconUrl("http://i.imgur.com/VzDRjUn.png");
            StringBuilder sb = new StringBuilder();
            if (command == null)
            {
                foreach (ModuleInfo mi in commands.Modules.OrderBy(x => x.Name))
                    if (!mi.IsSubmodule)
                        if (mi.Name != "Help")
                        {
                            bool ok = true;
                            foreach (PreconditionAttribute precondition in mi.Preconditions)
                                if (!(await precondition.CheckPermissions(context, null, services)).IsSuccess)
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
                                        if (!(await precondition.CheckPermissions(context, o as CommandInfo, services)).IsSuccess)
                                            cmds.Remove(o);
                                }
                                if (cmds.Count != 0)
                                {
                                    var list = cmds.Select(x => $"{((x as CommandInfo)?.Name ?? (x as ModuleInfo)?.Name)}").OrderBy(x => x);
                                    sb.AppendLine($"**{mi.Name}:** {String.Join(", ", list)}");
                                }
                            }
                        }

                eb.AddField((x) =>
                {
                    x.IsInline = false;
                    x.Name = "Query help";
                    x.Value = $"Usage: {context.Client.CurrentUser.Mention} [query]";
                });
                eb.AddField((x) =>
                {
                    x.IsInline = true;
                    x.Name = "Keywords";
                    x.Value = "search, first, method, type,\nproperty, event, in";
                });
                eb.AddField((x) =>
                {
                    x.IsInline = true;
                    x.Name = "Examples";
                    x.Value = "EmbedBuilder\n" +
                              "ModifyAsync in IRole\n" +
                              "search send message first method\n" +
                              "IGuildUser.Nickname";
                });
                eb.Footer = new EmbedFooterBuilder().WithText("Note: (i) = Inherited");
                eb.Description = sb.ToString();
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
                    if (cmd != null && (await cmd.Value.CheckPreconditionsAsync(context, services)).IsSuccess)
                    {
                        eb.Author.Name = $"Help: {cmd.Value.Command.Aliases.First()}";
                        sb.Append($"Usage: {MainHandler.Prefix}{cmd.Value.Command.Aliases.First()}");
                        if (cmd.Value.Command.Parameters.Count != 0)
                            sb.Append($" [{String.Join("] [", cmd.Value.Command.Parameters.Select(x => x.Name))}]");
                        if (!String.IsNullOrEmpty(cmd.Value.Command.Summary))
                            sb.Append($"\nSummary: {cmd.Value.Command.Summary}");
                        if (!String.IsNullOrEmpty(cmd.Value.Command.Remarks))
                            sb.Append($"\nRemarks: {cmd.Value.Command.Remarks}");
                        if (cmd.Value.Command.Aliases.Count != 1)
                            sb.Append($"\nAliases: {String.Join(", ", cmd.Value.Command.Aliases.Where(x => x != cmd.Value.Command.Aliases.First()))}");
                        eb.Description = sb.ToString();
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
