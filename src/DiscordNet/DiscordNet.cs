using Discord;
using Discord.WebSocket;
using DiscordNet.Github;
using Microsoft.Extensions.DependencyInjection;
using Paginator;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace DiscordNet
{
    public class DiscordNet
    {
        private DiscordSocketClient _client;
        private MainController _mainController;

        private Regex _githubRegex = new Regex("(?<=\\s|^)##(?<number>[0-9]{1,4})(?=\\s|$)", RegexOptions.Compiled | RegexOptions.ECMAScript);

        public async Task RunAsync()
        {
            string discordToken = await File.ReadAllTextAsync("Tokens/Discord.txt");
            string githubToken = await File.ReadAllTextAsync("Tokens/Github.txt");

            var githubRest = new GithubRest(githubToken);

            _client = new DiscordSocketClient(new DiscordSocketConfig()
            {
                LogLevel = LogSeverity.Info,
            });

            _client.Log += (message) =>
            {
                Console.WriteLine(message);
                return Task.CompletedTask;
            };

            _client.Ready += () =>
            {
                Console.WriteLine("Connected!");
                return Task.CompletedTask;
            };

            _client.Disconnected += (exception) => //Kills the bot if it doesn't reconnect
            {
                Task.Run(async () =>
                {
                    CancellationTokenSource cts = new CancellationTokenSource();
                    Task ctsTask()
                    {
                        cts.Cancel();
                        return Task.CompletedTask;
                    }

                    _client.Connected += ctsTask;
                    try { await Task.Delay(30000, cts.Token); } catch { }
                    if (!cts.IsCancellationRequested)
                        Environment.Exit(1);
                    _client.Connected -= ctsTask;
                    cts.Dispose();
                });
                return Task.CompletedTask;
            };

            _client.MessageReceived += (message) =>
            {
                Task.Run(async () =>
                {
                    if (message is IUserMessage userMessage)
                    {
                        if (userMessage.Author.IsBot)
                            return;
                        if (userMessage.Channel is ITextChannel tc && tc.GuildId == 81384788765712384 && userMessage.Channel.Name != "dotnet_discord-net" && userMessage.Channel.Name != "testing" && userMessage.Channel.Name != "playground")
                            return;
                        MatchCollection matches = _githubRegex.Matches(userMessage.Content);
                        if (matches.Count > 0)
                        {
                            var urls = await githubRest.GetIssuesUrlsAsync(matches.Take(3).Select(x => x.Groups["number"].Value));
                            await userMessage.Channel.SendMessageAsync(string.Join("\n", urls));
                        }
                    }
                });
                return Task.CompletedTask;
            };

            var services = new ServiceCollection();
            services.AddSingleton(_client);
            services.AddSingleton(new PaginatorService(_client));
            services.AddSingleton(githubRest);

            _mainController = new MainController(_client, services.BuildServiceProvider());
            await _mainController.InitializeEarlyAsync();

            await _client.LoginAsync(TokenType.Bot, discordToken);
            await _client.StartAsync();
            await Task.Delay(-1);
        }
    }
}
