using Discord;
using Discord.Addons.Paginator;
using Discord.Commands;
using Discord.WebSocket;
using DiscordNet.Controllers;
using DiscordNet.EmbedExtension;
using DiscordNet.Modules.Addons;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DiscordNet.Handlers
{
    public class CommandHandler
    {
        private CommandService _commands;
        private DiscordSocketClient _client;
        private IServiceProvider _services;
        private MainHandler _mainHandler;
        private MemoryCache cache = new MemoryCache(new MemoryCacheOptions { ExpirationScanFrequency = TimeSpan.FromMinutes(3) });

        public async Task InitializeAsync(MainHandler MainHandler, IServiceProvider services)
        {
            _mainHandler = MainHandler;
            _client = (DiscordSocketClient)services.GetService(typeof(DiscordSocketClient));
            _commands = new CommandService();
            _services = services;

            await _commands.AddModulesAsync(Assembly.GetEntryAssembly());
            _commands.Log += Log;

            _client.MessageReceived += HandleCommand;
            _client.MessageUpdated += HandleUpdate;
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.Exception);
            return Task.CompletedTask;
        }

        public Task Close()
        {
            _client.MessageReceived -= HandleCommand;
            return Task.CompletedTask;
        }

        private Task HandleUpdate(Cacheable<IMessage, ulong> before, SocketMessage after, ISocketMessageChannel channel)
        {
            if (!(after is SocketUserMessage afterSocket))
                return Task.CompletedTask;
            _ = Task.Run(async () =>
            {
                ulong? id;
                if ((id = GetOurMessageIdFromCache(before.Id)) != null)
                {
                    var botMessage = await channel.GetMessageAsync(id.Value) as IUserMessage;
                    if (botMessage == null)
                        return;
                    int argPos = 0;
                    if (!afterSocket.HasMentionPrefix(_client.CurrentUser, ref argPos)) return;
                    var reply = await BuildReply(afterSocket, after.Content.Substring(argPos));

                    if (reply.Item1 == null && reply.Item2 == null && reply.Item3 == null)
                        return;
                    var pagination = (PaginationService)_services.GetService(typeof(PaginationService));
                    var isPaginatedMessage = pagination.IsPaginatedMessage(id.Value);
                    if (reply.Item3 != null)
                    {
                        if (isPaginatedMessage)
                            await pagination.UpdatePaginatedMessageAsync(botMessage, reply.Item3);
                        else
                            await pagination.EditMessageToPaginatedMessageAsync(botMessage, reply.Item3);
                    }
                    else
                    {
                        if (isPaginatedMessage)
                        {
                            pagination.StopTrackingPaginatedMessage(id.Value);
                            _ = botMessage.RemoveAllReactionsAsync();
                        }
                        await botMessage.ModifyAsync(x => { x.Content = reply.Item1; x.Embed = reply.Item2?.Build(); });
                    }
                }
            });
            return Task.CompletedTask;
        }

        public Task HandleCommand(SocketMessage parameterMessage)
        {
            var msg = parameterMessage as SocketUserMessage;
            if (msg == null) return Task.CompletedTask;
            if (msg.Author.IsBot) return Task.CompletedTask;
            if (msg.Channel is ITextChannel tc && tc.GuildId == 81384788765712384)
                if (msg.Channel.Name != "dotnet_discord-net" && msg.Channel.Name != "testing" && msg.Channel.Name != "playground") return Task.CompletedTask;
            int argPos = 0;
            if (!(msg.HasMentionPrefix(_client.CurrentUser, ref argPos) /*|| msg.HasStringPrefix(MainHandler.Prefix, ref argPos)*/)) return Task.CompletedTask;
            _ = HandleCommandAsync(msg, argPos);
            return Task.CompletedTask;
        }

        public async Task HandleCommandAsync(SocketUserMessage msg, int argPos)
        {
            var reply = await BuildReply(msg, msg.Content.Substring(argPos));
            if (reply.Item1 == null && reply.Item2 == null && reply.Item3 == null)
                return;
            IUserMessage message;
            if (reply.Item3 != null)
                message = await ((PaginationService)_services.GetService(typeof(PaginationService))).SendPaginatedMessageAsync(msg.Channel, reply.Item3);
            else
                message = await msg.Channel.SendMessageAsync(reply.Item1, embed: reply.Item2?.Build());
            AddCache(msg.Id, message.Id);
        }

        private async Task<(string, EmbedBuilder, PaginatedMessage)> BuildReply(IUserMessage msg, string message)
        {
            var context = new MyCommandContext(_client, _mainHandler, msg);
            var result = await _commands.ExecuteAsync(context, message, _services);
            if (!result.IsSuccess && result.Error != CommandError.UnknownCommand)
                return (result.ErrorReason, null, null);
            else if (!result.IsSuccess)
            {
                if (!_mainHandler.QueryHandler.IsReady())
                {
                    return ("Loading cache...", null, null); //TODO: Change message
                }
                else
                {
                    try
                    {
                        var tuple = await _mainHandler.QueryHandler.RunAsync(message);
                        if (tuple.Item2 is PaginatorBuilder pag)
                        {
                            var paginated = new PaginatedMessage(pag.Pages, "Results", user: msg.Author, options: new AppearanceOptions { Timeout = TimeSpan.FromMinutes(10) });
                            return (null, null, paginated);
                        }
                        else
                            return (tuple.Item1, tuple.Item2, null);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                        return ("Uh-oh... I think some pipes have broken...", null, null);
                    }
                }
            }
            return (null, null, null);
        }

        public void AddCache(ulong userMessageId, ulong ourMessageId)
        {
            cache.Set(userMessageId, ourMessageId, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) });
        }

        public ulong? GetOurMessageIdFromCache(ulong messageId)
        {
            if (cache.TryGetValue<ulong>(messageId, out ulong id))
                return id;
            return null;
        }

        public async Task<EmbedBuilder> HelpEmbedBuilderAsync(ICommandContext context, string command = null)
        {
            EmbedBuilder eb = new EmbedBuilder();
            eb.Author = new EmbedAuthorBuilder().WithName("Help:").WithIconUrl("http://i.imgur.com/VzDRjUn.png");
            StringBuilder sb = new StringBuilder();
            if (command == null)
            {
                foreach (ModuleInfo mi in _commands.Modules.OrderBy(x => x.Name))
                    if (!mi.IsSubmodule)
                        if (mi.Name != "Help")
                        {
                            bool ok = true;
                            foreach (PreconditionAttribute precondition in mi.Preconditions)
                                if (!(await precondition.CheckPermissions(context, null, _services)).IsSuccess)
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
                                        if (!(await precondition.CheckPermissions(context, o as CommandInfo, _services)).IsSuccess)
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
                    x.Value = "method, type, property,\nevent, in, list";
                });
                eb.AddField((x) =>
                {
                    x.IsInline = true;
                    x.Name = "Examples";
                    x.Value = "EmbedBuilder\n" +
                              "IGuildUser.Nickname\n" +
                              "ModifyAsync in IRole\n" +
                              "send message\n" +
                              "type Emote";
                });
                eb.Footer = new EmbedFooterBuilder().WithText("Note: (i) = Inherited");
                eb.Description = sb.ToString();
            }
            else
            {
                SearchResult sr = _commands.Search(context, command);
                if (sr.IsSuccess)
                {
                    Nullable<CommandMatch> cmd = null;
                    if (sr.Commands.Count == 1)
                        cmd = sr.Commands.First();
                    else
                    {
                        int lastIndex;
                        var find = sr.Commands.Where(x => x.Command.Aliases.First().Equals(command, StringComparison.OrdinalIgnoreCase));
                        if (find.Any())
                            cmd = find.First();
                        while (cmd == null && (lastIndex = command.LastIndexOf(' ')) != -1) //TODO: Maybe remove and say command not found?
                        {
                            find = sr.Commands.Where(x => x.Command.Aliases.First().Equals(command.Substring(0, lastIndex), StringComparison.OrdinalIgnoreCase));
                            if (find.Any())
                                cmd = find.First();
                            command = command.Substring(0, lastIndex);
                        }
                    }
                    if (cmd != null && (await cmd.Value.CheckPreconditionsAsync(context, _services)).IsSuccess)
                    {
                        eb.Author.Name = $"Help: {cmd.Value.Command.Aliases.First()}";
                        sb.Append($"Usage: {_mainHandler.Prefix}{cmd.Value.Command.Aliases.First()}");
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
