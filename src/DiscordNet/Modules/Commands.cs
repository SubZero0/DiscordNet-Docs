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
using System.Text;
using System.Threading.Tasks;

namespace DiscordNet.Modules
{
    [Name("Commands")]
    public class GeneralCommands : ModuleBase<MyCommandContext>
    {
        [Command("clean")]
        [Summary("Delete all the messages from this bot within the last X messages")]
        public async Task Clean(int messages = 30)
        {
            if (messages > 50)
                messages = 50;
            else if (messages < 2)
                messages = 2;
            var msgs = await Context.Channel.GetMessagesAsync(messages).Flatten();
            msgs = msgs.Where(x => x.Author.Id == Context.Client.CurrentUser.Id);
            foreach (IMessage msg in msgs)
                await msg.DeleteAsync();
        }

        [Command("docs")]
        [Summary("Show the docs url")]
        public async Task Docs()
        {
            await ReplyAsync("Docs: https://discord.foxbot.me/docs/");
        }

        [Command("guides")]
        [Alias("guide")]
        [Summary("Show the url of a guide")]
        public async Task Guides([Remainder] string guide = null)
        {
            string html;
            using (var httpClient = new HttpClient())
            {
                var res = await httpClient.GetAsync("https://raw.githubusercontent.com/RogueException/Discord.Net/dev/docs/guides/toc.yml");
                if (!res.IsSuccessStatusCode)
                    throw new Exception($"An error occurred: {res.ReasonPhrase}");
                html = await res.Content.ReadAsStringAsync();
            }
            Dictionary<string, Dictionary<string, string>> guides = new Dictionary<string, Dictionary<string, string>>();
            var separate = html.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
            string lastname = "";
            for (int i = 0; i < separate.Length; i++)
            {
                string line = separate[i].Trim();
                if (line.StartsWith("- name:"))
                {
                    lastname = line.Split(new string[] { "- name:" }, StringSplitOptions.None)[1].Trim();
                }
                else if (line.StartsWith("items:"))
                {
                    guides[lastname] = new Dictionary<string, string>();
                }
                else if (line.StartsWith("href:"))
                {
                    string link = line.Split(new string[] { "href:" }, StringSplitOptions.None)[1].Trim();
                    guides.Last().Value[lastname] = $"{link.Substring(0, link.Length - 2)}html";
                }
            }
            StringBuilder sb = new StringBuilder();
            EmbedAuthorBuilder eab = new EmbedAuthorBuilder { IconUrl = Context.Client.CurrentUser.GetAvatarUrl() };
            if (guide == null)
            {
                eab.Name = "Guides";
                foreach (string category in guides.Keys)
                {
                    sb.AppendLine($"**{category}**");
                    foreach (string subcategory in guides[category].Keys)
                        sb.AppendLine($"- [{subcategory}](https://discord.foxbot.me/docs/guides/{guides[category][subcategory]})");
                }
            }
            else
            {
                guide = guide.ToLower();
                foreach (string category in guides.Keys)
                {
                    eab.Name = $"Guide: {category}";
                    bool add = false;
                    if (category.IndexOf(guide, StringComparison.OrdinalIgnoreCase) != -1)
                        add = true;
                    else
                        foreach (string subcategory in guides[category].Keys)
                            if (subcategory.IndexOf(guide, StringComparison.OrdinalIgnoreCase) != -1)
                                add = true;
                    if(add)
                    {
                        foreach (string subcategory in guides[category].Keys)
                            sb.AppendLine($"- [{subcategory}](https://discord.foxbot.me/docs/guides/{guides[category][subcategory]})");
                        break;
                    }
                }
            }
            string result = sb.ToString();
            if (string.IsNullOrEmpty(result))
            {
                await ReplyAsync("No guide found.");
            }
            else
            {
                EmbedBuilder eb = new EmbedBuilder()
                {
                    Author = eab,
                    Description = result
                };
                await ReplyAsync("", embed: eb);
            }
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
            eb.Author = new EmbedAuthorBuilder().WithName(name).WithIconUrl(Context.Client.CurrentUser.GetAvatarUrl());
            eb.ThumbnailUrl = Context.Client.CurrentUser.GetAvatarUrl();
            eb.AddField(x =>
            {
                x.IsInline = false;
                x.Name = "Info";
                x.Value = $"- Library: Discord.Net ({DiscordConfig.Version})\n" +
                          $"- Runtime: {RuntimeInformation.FrameworkDescription} {RuntimeInformation.ProcessArchitecture}\n" +
                          $"- Source: https://github.com/SubZero0/DiscordNet-Docs\n" +
                          $"- Uptime: {(DateTime.Now - Process.GetCurrentProcess().StartTime).ToString(@"dd\.hh\:mm\:ss")}";
            });
            eb.AddField(x =>
            {
                x.IsInline = true;
                x.Name = "Docs";
                x.Value = $"- Types: {Context.MainHandler.QueryHandler.Cache.GetTypeCount()}\n" +
                          $"- Methods: {Context.MainHandler.QueryHandler.Cache.GetMethodCount()}\n" +
                          $"- Properties: {Context.MainHandler.QueryHandler.Cache.GetPropertyCount()}";
            });
            eb.AddField(x =>
            {
                x.IsInline = true;
                x.Name = "​"; // <- zero-width space here
                x.Value = $"- Events: {Context.MainHandler.QueryHandler.Cache.GetEventCount()}\n" +
                          $"- Extension types: {Context.MainHandler.QueryHandler.Cache.GetExtensionTypesCount()}\n" +
                          $"- Extension methods: {Context.MainHandler.QueryHandler.Cache.GetExtensioMethodsCount()}";
            });
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