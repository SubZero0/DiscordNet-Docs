using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordNet.Controllers;
using System;
using System.Threading.Tasks;

namespace DiscordNet
{
    public class DiscordNet
    {
        private DiscordSocketClient Discord;
        private MainHandler MainHandler;

        public async Task RunAsync()
        {
            Discord = new DiscordSocketClient(new DiscordSocketConfig()
            {
                LogLevel = LogSeverity.Info
            });

            Discord.Log += (message) =>
            {
                Console.WriteLine($"{message.ToString()}");
                return Task.CompletedTask;
            };
            Discord.Ready += () =>
            {
                Console.WriteLine($"Connected!");
                return Task.CompletedTask;
            };

            var map = new DependencyMap();
            map.Add(Discord);

            MainHandler = new MainHandler(Discord);
            await MainHandler.InitializeEarlyAsync(map);

            await Discord.LoginAsync(TokenType.Bot, "...");
            await Discord.StartAsync();
            await Task.Delay(-1);
        }
    }
}
