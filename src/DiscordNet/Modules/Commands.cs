using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordNet.Github;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace DiscordNet.Modules
{
    [Name("Commands")]
    public class GeneralCommands : ModuleBase<BotCommandContext>
    {
        private static string BuildsDirectory { get; } = "Builds";

        public GithubRest GithubRest { get; set; }

        [Command("clean")]
        [Summary("Delete all the messages from this bot within the last X messages")]
        public async Task Clean(int messages = 30)
        {
            if (messages > 100)
                messages = 100;
            else if (messages < 2)
                messages = 2;
            var msgs = await Context.Channel.GetMessagesAsync(messages).FlattenAsync();
            msgs = msgs.Where(x => x.Author.Id == Context.Client.CurrentUser.Id);
            foreach (IMessage msg in msgs)
                await msg.DeleteAsync();
        }

        [Command("docs")]
        [Summary("Show the docs url")]
        public Task Docs()
            => ReplyAsync($"Docs: {DocsUrlHandler.DocsBaseUrl}");

        [Command("invite")]
        [Summary("Show the invite url")]
        public Task Invite()
            => ReplyAsync($"Invite: https://discordapp.com/oauth2/authorize?client_id=274366085011079169&scope=bot");

        [Command("artifacts")]
        [Alias("getartifacts", "pr")]
        public async Task Artifacts(int prNumber)
        {
            string path = Path.Combine(BuildsDirectory, $"{prNumber}.zip");
            if (File.Exists(path))
                await Context.Channel.SendFileAsync(path, $"Generated at: {File.GetCreationTimeUtc(path):dd MMM yyy HH:mm:ss} UTC");
            else
            {
                var appInfo = await Context.Client.GetApplicationInfoAsync();
                await ReplyAsync($"There's no artifacts generated for a pull request numbered {prNumber}.\n" +
                                 $"You can ask the bot owner ({appInfo.Owner}) to generate them.");
            }
        }

        [Command("delartifacts")]
        [Alias("delpr", "pr")]
        [RequireOwner]
        public async Task DelArtifacts(string prNumber)
        {
            if (prNumber == "all")
            {
                Directory.Delete(BuildsDirectory, true);
                await ReplyAsync("Deleted all generated artifacts.");
            }
            else if (prNumber == "old")
            {
                int filesDeleted = 0;
                foreach (var file in Directory.GetFiles(BuildsDirectory))
                    if (DateTime.UtcNow - File.GetCreationTimeUtc(file) > TimeSpan.FromDays(180))
                    {
                        File.Delete(file);
                        filesDeleted++;
                    }
                await ReplyAsync($"Deleted {filesDeleted} generated artifacts.");
            }
            else
            {
                string path = Path.Combine(BuildsDirectory, $"{prNumber}.zip");
                if (File.Exists(path))
                {
                    File.Delete(path);
                    if (Directory.GetFiles(BuildsDirectory).Length == 0)
                        Directory.Delete(BuildsDirectory);
                    await ReplyAsync($"Deleted artifacts for pull request #{prNumber}.");
                }
                else
                    await ReplyAsync($"There's no artifacts generated for a pull request numbered {prNumber}.");
            }
        }

        [Command("genartifacts", RunMode = RunMode.Async)]
        [Alias("genpr")]
        [RequireTrustedMember]
        public async Task GenArtifacts(int prNumber)
        {
            var pr = await GithubRest.GetPullRequestAsync(prNumber.ToString());
            if (pr == null)
            {
                await ReplyAsync("Pull request not found.");
                return;
            }

            string filePath = Path.Combine(BuildsDirectory, $"{prNumber}.zip");
            string zipPath = Path.Combine(BuildsDirectory, $"{prNumber}.zip");
            string unzipPath = Path.Combine(BuildsDirectory, $"{prNumber}");
            string artifactsPath = Path.GetFullPath(Path.Combine(unzipPath, "artifacts"));

            if (File.Exists(zipPath))
            {
                var emoji = new Emoji("✅");
                var tokenSource = new CancellationTokenSource();
                bool reacted = false;
                Task WaitReaction(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction reaction)
                {
                    if (reaction.UserId == Context.User.Id && reaction.Emote.Equals(emoji))
                    {
                        reacted = true;
                        tokenSource.Cancel();
                    }

                    return Task.CompletedTask;
                };

                var msg = await ReplyAsync($"There's already generated artifacts for that PR, do you want to recreate them?");
                await msg.AddReactionAsync(emoji);
                Context.Client.ReactionAdded += WaitReaction;
                try { await Task.Delay(30000, tokenSource.Token); } catch { }
                Context.Client.ReactionAdded -= WaitReaction;
                tokenSource.Dispose();

                if (!reacted)
                {
                    await msg.RemoveReactionAsync(emoji, Context.Client.CurrentUser);
                    return;
                }

                File.Delete(zipPath);
            }

            var typing = Context.Channel.EnterTypingState();
            try
            {
                Directory.CreateDirectory(BuildsDirectory);
                //Download branch zip
                using (var stream = await GithubRest.GetRepositoryDownloadStreamAsync(pr))
                {
                    using (var file = File.Create(filePath))
                    {
                        stream.Seek(0, SeekOrigin.Begin);
                        await stream.CopyToAsync(file);
                    }
                }
                //Unzip branch
                ZipFile.ExtractToDirectory(filePath, unzipPath);
                string unzippedFolder = Directory.GetDirectories(unzipPath)[0];
                //Delete zip
                File.Delete(filePath);
                //Pack
                Directory.CreateDirectory(artifactsPath);
                string suffix = $"{prNumber}";
                Pack("src\\Discord.Net.Core\\Discord.Net.Core.csproj", suffix, artifactsPath, unzippedFolder);
                Pack("src\\Discord.Net.Rest\\Discord.Net.Rest.csproj", suffix, artifactsPath, unzippedFolder);
                Pack("src\\Discord.Net.Commands\\Discord.Net.Commands.csproj", suffix, artifactsPath, unzippedFolder);
                Pack("src\\Discord.Net.WebSocket\\Discord.Net.WebSocket.csproj", suffix, artifactsPath, unzippedFolder);
                Pack("src\\Discord.Net.Webhook\\Discord.Net.Webhook.csproj", suffix, artifactsPath, unzippedFolder);
                //Zip packs
                ZipFile.CreateFromDirectory(artifactsPath, zipPath);
                //Clean
                Directory.Delete(unzipPath, true);

                await Context.Channel.SendFileAsync(zipPath);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                await ReplyAsync("Uh... something broke.");
            }
            finally
            {
                typing?.Dispose();
            }
        }
        private void Pack(string project, string suffix, string outputDir, string workingDir)
        {
            var procStartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"pack {project} -o \"{outputDir}\" --version-suffix {suffix} -c Release",
                WorkingDirectory = workingDir,
                UseShellExecute = false,
                CreateNoWindow = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            var pr = new Process
            {
                StartInfo = procStartInfo
            };
            pr.Start();
            pr.WaitForExit();
        }

        /*[Command("guides")]
        [Alias("guide")]
        [Summary("Show the url of a guide")]
        public async Task Guides([Remainder] string guide = null)
        {
            string html;
            using (var httpClient = new HttpClient())
            {
                var res = await httpClient.GetAsync("https://raw.githubusercontent.com/discord-net/Discord.Net/dev/docs/guides/toc.yml");
                if (!res.IsSuccessStatusCode)
                    throw new Exception($"An error occurred: {res.ReasonPhrase}");
                html = await res.Content.ReadAsStringAsync();
            }
            var guides = new Dictionary<string, string>();
            var subguides = new Dictionary<string, Dictionary<string, string>>();
            var separate = html.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
            string lastname = "";
            for (int i = 0; i < separate.Length; i++)
            {
                string line = separate[i].Trim();
                if (line.StartsWith("- name:"))
                {
                    lastname = line.Split(new[] { "- name:" }, StringSplitOptions.None)[1].Trim();
                }
                else if (line.StartsWith("items:"))
                {
                    guides[lastname] = null;
                    subguides[lastname] = new Dictionary<string, string>();
                }
                else if (line.StartsWith("href:"))
                {
                    string link = line.Split(new[] { "href:" }, StringSplitOptions.None)[1].Trim();
                    if (separate[i].StartsWith("   ")) //TODO: Change how to find subgroups
                        subguides.Last().Value[lastname] = $"{link.Substring(0, link.Length - 2)}html";
                    else
                        guides[lastname] = $"{link.Substring(0, link.Length - 2)}html";
                }
            }
            StringBuilder sb = new StringBuilder();
            EmbedAuthorBuilder eab = new EmbedAuthorBuilder { IconUrl = Context.Client.CurrentUser.GetAvatarUrl() };
            if (string.IsNullOrEmpty(guide))
            {
                eab.Name = "Guides";
                foreach (string category in guides.Keys)
                {
                    if (guides[category] != null)
                        sb.AppendLine($"[**{category}**]({guides[category]})");
                    else
                        sb.AppendLine($"**{category}**");
                    if (subguides.ContainsKey(category))
                        foreach (string subcategory in subguides[category].Keys)
                            sb.AppendLine($"- [{subcategory}]({DocsUrlHandler.DocsBaseUrl}guides/{subguides[category][subcategory]})");
                }
            }
            else
            {
                guide = guide.ToLower();
                foreach (string category in guides.Keys)
                {
                    eab.Name = $"Guide: {category}";
                    if (guides.ContainsKey(category))
                        eab.Url = guides[category];
                    bool add = false;
                    if (category.IndexOf(guide, StringComparison.OrdinalIgnoreCase) != -1)
                        add = true;
                    else
                        foreach (string subcategory in subguides[category].Keys)
                            if (subcategory.IndexOf(guide, StringComparison.OrdinalIgnoreCase) != -1)
                                add = true;
                    if (add)
                    {
                        foreach (string subcategory in subguides[category].Keys)
                            sb.AppendLine($"- [{subcategory}]({DocsUrlHandler.DocsBaseUrl}guides/{subguides[category][subcategory]})");
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
                await ReplyAsync("", embed: eb.Build());
            }
        }*/

        [Command("info")]
        [Summary("Show some information")]
        public async Task Info()
        {
            var application = await Context.Client.GetApplicationInfoAsync();
            EmbedBuilder eb = new EmbedBuilder();
            string name;
            if (Context.Guild != null)
                name = Context.Guild.CurrentUser.Nickname ?? Context.Guild.CurrentUser.Username;
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
                x.Value = $"- Types: {Context.MainController.QueryHandler.Cache.GetTypeCount()}\n" +
                          $"- Methods: {Context.MainController.QueryHandler.Cache.GetMethodCount()}\n" +
                          $"- Properties: {Context.MainController.QueryHandler.Cache.GetPropertyCount()}";
            });
            eb.AddField(x =>
            {
                x.IsInline = true;
                x.Name = "​"; // <- zero-width space here
                x.Value = $"- Events: {Context.MainController.QueryHandler.Cache.GetEventCount()}\n" +
                          $"- Extension types: {Context.MainController.QueryHandler.Cache.GetExtensionTypesCount()}\n" +
                          $"- Extension methods: {Context.MainController.QueryHandler.Cache.GetExtensioMethodsCount()}";
            });
            await ReplyAsync("", false, eb.Build());
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
                        await ReplyAsync("", embed: new EmbedBuilder().WithTitle("Result:").WithDescription(o.ToString()).Build());
                }
                catch (Exception e)
                {
                    await ReplyAsync("", embed: new EmbedBuilder().WithTitle("Error:").WithDescription($"{e.GetType().ToString()}: {e.Message}\nFrom: {e.Source}").Build());
                }
            }
        }

        [Command("setdocsurl")]
        [RequireOwner]
        public async Task SetDocsUrl([Remainder] string url)
        {
            if (!url.EndsWith("/"))
                url = url + "/";
            DocsUrlHandler.DocsBaseUrl = url;
            await ReplyAsync($"Changed base docs url to: <{url}>");
        }

        [Command("botavatar")]
        [RequireOwner]
        public async Task Avatar(string url)
        {
            MemoryStream imgStream = null;
            try
            {
                using (var http = new HttpClient())
                {
                    using (var sr = await http.GetStreamAsync(url))
                    {
                        imgStream = new MemoryStream();
                        await sr.CopyToAsync(imgStream);
                        imgStream.Position = 0;
                    }
                }
            }
            catch (Exception)
            {
                await ReplyAsync("Something went wrong while downloading the image.");
                return;
            }
            await Context.Client.CurrentUser.ModifyAsync(x => x.Avatar = new Image(imgStream));
            await ReplyAsync("Avatar changed!");
            imgStream.Dispose();
        }
    }

    [Name("Help")]
    public class HelpCommand : ModuleBase<BotCommandContext>
    {
        [Command("help")]
        [Summary("Shows the help command")]
        public async Task Help([Remainder] string command = null)
            => await ReplyAsync("", embed: (await Context.MainController.CommandHandler.HelpEmbedBuilderAsync(Context, command)).Build());
    }
}