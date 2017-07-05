using Discord;
using Discord.Addons.Paginator;
using Discord.WebSocket;
using DiscordNet.Controllers;
using Microsoft.Extensions.DependencyInjection;
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
                Console.WriteLine(message);
                return Task.CompletedTask;
            };
            Discord.Ready += () =>
            {
                Console.WriteLine("Connected!");
                return Task.CompletedTask;
            };

            var services = new ServiceCollection();
            services.AddSingleton(Discord);
            services.AddPaginator(Discord);

            MainHandler = new MainHandler(Discord, services.BuildServiceProvider());
            await MainHandler.InitializeEarlyAsync();

            await Discord.LoginAsync(TokenType.Bot, "...");
            await Discord.StartAsync();
            await Task.Delay(-1);
        }
    }


}
