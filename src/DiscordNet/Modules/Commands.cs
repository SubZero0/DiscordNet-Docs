using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordNet.Modules.Addons;
using DiscordNet.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace DiscordNet.Modules
{
    [Name("General")]
    public class GeneralCommands : ModuleBase<MyCommandContext>
    {
        [Command("clean")]
        [Summary("Delete all the messages from this bot within the last X messages")]
        public async Task Clean(int messages = 30)
        {
            var msgs = await Context.Channel.GetMessagesAsync(messages).Flatten();
            msgs = msgs.Where(x => x.Author.Id == Context.Client.CurrentUser.Id);
            foreach (IMessage msg in msgs)
                await msg.DeleteAsync();
        }

        [Command("source")]
        [Summary("Source code location")]
        public async Task Source()
        {
            await ReplyAsync("Source: https://github.com/SubZero0/DiscordNet-Docs");
        }

        [Command("info")]
        [Summary("Show some information")]
        public async Task Info()
        {
            var application = await Context.Client.GetApplicationInfoAsync();
            EmbedBuilder eb = new EmbedBuilder();
            string name;
            if (Context.Guild != null)
            {
                var user = await Context.Guild.GetCurrentUserAsync();
                name = user.Nickname ?? user.Username;
            }
            else
                name = Context.Client.CurrentUser.Username;
            eb.Author = new EmbedAuthorBuilder().WithName(name).WithIconUrl(Context.Client.CurrentUser.AvatarUrl);
            eb.ThumbnailUrl = Context.Client.CurrentUser.AvatarUrl;
            eb.Description = $"{Format.Bold("Info")}\n" +
                                $"- Library: Discord.Net ({DiscordConfig.Version})\n" +
                                $"- Runtime: {RuntimeInformation.FrameworkDescription} {RuntimeInformation.OSArchitecture}\n" +
                                $"- Uptime: {(DateTime.Now - Process.GetCurrentProcess().StartTime).ToString(@"dd\.hh\:mm\:ss")}\n\n" +

                                $"{Format.Bold("Docs")}\n" +
                                $"- Types: {Context.MainHandler.QueryHandler.Cache.GetTypeCount()}\n" +
                                $"- Methods: {Context.MainHandler.QueryHandler.Cache.GetMethodCount()}\n" +
                                $"- Properties: {Context.MainHandler.QueryHandler.Cache.GetPropertyCount()}\n";
            await ReplyAsync("", false, eb);
        }

        [Command("eval", RunMode = RunMode.Async)] //TODO: Safe eval ? 👀
        [RequireOwner]
        public async Task Eval([Remainder] string code)
        {
            using (Context.Channel.EnterTypingState())
            {
                try
                {
                    var references = new List<MetadataReference>();
                    var referencedAssemblies = Assembly.GetEntryAssembly().GetReferencedAssemblies();
                    foreach (var referencedAssembly in referencedAssemblies)
                        references.Add(MetadataReference.CreateFromFile(Assembly.Load(referencedAssembly).Location));
                    var scriptoptions = ScriptOptions.Default.WithReferences(references);
                    Globals globals = new Globals { Context = Context, Guild = Context.Guild as SocketGuild };
                    object o = await CSharpScript.EvaluateAsync(@"using System;using System.Linq;using System.Threading.Tasks;using Discord.WebSocket;using Discord;" + @code, scriptoptions, globals);
                    if (o == null)
                        await ReplyAsync("Done!");
                    else
                        await ReplyAsync("", embed: new EmbedBuilder().WithTitle("Result:").WithDescription(o.ToString()));
                }
                catch (Exception e)
                {
                    await ReplyAsync("", embed: new EmbedBuilder().WithTitle("Error:").WithDescription($"{e.GetType().ToString()}: {e.Message}\nFrom: {e.Source}"));
                }
            }
        }
    }

    [Name("Help")]
    public class HelpCommand : ModuleBase<MyCommandContext>
    {
        [Command("help")]
        [Summary("Shows the help command")]
        public async Task Help([Remainder] string command = null)
        {
            await ReplyAsync("", embed: await Context.MainHandler.CommandHandler.HelpEmbedBuilderAsync(Context, command));
        }
    }
}