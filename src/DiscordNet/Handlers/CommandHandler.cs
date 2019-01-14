using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordNet.Controllers;
using DiscordNet.EmbedExtension;
using DiscordNet.Modules.Addons;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Paginator;
using System;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DiscordNet.Handlers
{
    public class CommandHandler
    {
        private CommandService _commandService;
        private DiscordSocketClient _client;
        private IServiceProvider _services;
        private MainController _mainHandler;
        private MemoryCache _cache = new MemoryCache(new MemoryCacheOptions { ExpirationScanFrequency = TimeSpan.FromMinutes(3) });

        public async Task InitializeAsync(MainController MainHandler, IServiceProvider services)
        {
            _mainHandler = MainHandler;
            _client = services.GetService<DiscordSocketClient>();
            _commandService = new CommandService();
            _services = services;

            await _commandService.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
            _commandService.Log += Log;

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
                    if (!(await channel.GetMessageAsync(id.Value) is IUserMessage botMessage))
                        return;
                    int argPos = 0;
                    if (!afterSocket.HasMentionPrefix(_client.CurrentUser, ref argPos)) return;
                    var reply = await BuildReply(afterSocket, after.Content.Substring(argPos));

                    if (reply.Item1 == null && reply.Item2 == null && reply.Item3 == null)
                        return;
                    var pagination = _services.GetService<PaginatorService>();
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
                message = await (_services.GetService<PaginatorService>()).SendPaginatedMessageAsync(msg.Channel, reply.Item3);
            else
                message = await msg.Channel.SendMessageAsync(reply.Item1, embed: reply.Item2?.Build());
            AddCache(msg.Id, message.Id);
        }

        private async Task<(string, EmbedBuilder, PaginatedMessage)> BuildReply(IUserMessage msg, string message)
        {
            var context = new MyCommandContext(_client, _mainHandler, msg);
            var result = await _commandService.ExecuteAsync(context, message, _services);
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
                            var paginated = new PaginatedMessage(pag.Pages, PaginatedMessageActions.Simplified, "Results", user: msg.Author, options: new AppearanceOptions { TimeoutAfterLastAction = TimeSpan.FromMinutes(3) });
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
            => _cache.Set(userMessageId, ourMessageId, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) });

        public ulong? GetOurMessageIdFromCache(ulong messageId)
            => _cache.TryGetValue<ulong>(messageId, out ulong id) ? (ulong?)id : null;

        public async Task<EmbedBuilder> HelpEmbedBuilderAsync(ICommandContext context, string command = null)
        {
            EmbedBuilder eb = new EmbedBuilder();
            eb.Author = new EmbedAuthorBuilder().WithName("Help:").WithIconUrl("http://i.imgur.com/VzDRjUn.png");
            StringBuilder sb = new StringBuilder();
            if (command == null)
            {
                foreach (ModuleInfo mi in _commandService.Modules.OrderBy(x => x.Name))
                    if (!mi.IsSubmodule)
                        if (mi.Name != "Help")
                        {
                            bool ok = true;
                            foreach (PreconditionAttribute precondition in mi.Preconditions)
                                if (!(await precondition.CheckPermissionsAsync(context, null, _services)).IsSuccess)
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
                                        if (!(await precondition.CheckPermissionsAsync(context, o as CommandInfo, _services)).IsSuccess)
                                            cmds.Remove(o);
                                }
                                if (cmds.Count != 0)
                                {
                                    var list = cmds.Select(x => $"{((x as CommandInfo)?.Name ?? (x as ModuleInfo)?.Name)}").OrderBy(x => x);
                                    sb.AppendLine($"**{mi.Name}:** {string.Join(", ", list)}");
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
                SearchResult sr = _commandService.Search(context, command);
                if (sr.IsSuccess)
                {
                    CommandMatch? cmd = null;
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
                            sb.Append($" [{string.Join("] [", cmd.Value.Command.Parameters.Select(x => x.Name))}]");
                        if (!string.IsNullOrEmpty(cmd.Value.Command.Summary))
                            sb.Append($"\nSummary: {cmd.Value.Command.Summary}");
                        if (!string.IsNullOrEmpty(cmd.Value.Command.Remarks))
                            sb.Append($"\nRemarks: {cmd.Value.Command.Remarks}");
                        if (cmd.Value.Command.Aliases.Count != 1)
                            sb.Append($"\nAliases: {string.Join(", ", cmd.Value.Command.Aliases.Where(x => x != cmd.Value.Command.Aliases.First()))}");
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
