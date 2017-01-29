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
using System.Net.Http;
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

        [Command("guide")]
        [Summary("Show the url of a guide")]
        public async Task Guide([Remainder] string guide = null)
        {
            string html;
            using (var httpClient = new HttpClient())
            {
                var res = await httpClient.GetAsync("https://raw.githubusercontent.com/RogueException/Discord.Net/dev/docs/guides/toc.yml");
                if (!res.IsSuccessStatusCode)
                    throw new Exception($"An error occurred: {res.ReasonPhrase}");
                html = await res.Content.ReadAsStringAsync();
            }
            var separate = html.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
            Dictionary<string, string> guides = new Dictionary<string, string>();
            for (int i = 0; i < separate.Length; i += 2)
            {
                var key = separate[i].Split(':')[1].Trim();
                var value = separate[i+1].Split(':')[1].Trim();
                guides[key] = value;
            }
            if(guide == null)
                await ReplyAsync($"**Guides:** {string.Join(", ", guides.Select(x => x.Key))}\nChoose with: guide [name]");
            else
            {
                var results = guides.Where(x => x.Key.IndexOf(guide, StringComparison.OrdinalIgnoreCase) != -1);
                if(results.Count() != 1)
                    await ReplyAsync($"**Did you mean:** {string.Join(", ", results.Select(x => x.Key))}\nChoose with: guide [name]");
                else
                    await ReplyAsync($"{results.First().Key}: https://discord.foxbot.me/docs/guides/{results.First().Value.Replace(".md", ".html")}");
            }
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