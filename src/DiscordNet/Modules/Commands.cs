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
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace DiscordNet.Modules
{
    [Name("General")]
    public class GeneralCommands : ModuleBase<MyCommandContext>
    {
        [Command("docs")]
        public async Task Docs([Remainder] string query = null)
        {
            if (query == null)
            {
                await ReplyAsync("Docs: https://discord.foxbot.me/docs/ \nType ``docs [query]`` to search the docs.");
            }
            else if(query == "help")
            {
                EmbedBuilder eb = new EmbedBuilder();
                eb.Title = "Docs Help";
                eb.Description = "Usage: docs [query]";
                eb.AddField((x) =>
                {
                    x.IsInline = false;
                    x.Name = "Keywords";
                    x.Value = "search, first, method, type, property, in";
                });
                eb.AddField((x) =>
                {
                    x.IsInline = false;
                    x.Name = "Examples";
                    x.Value = "docs EmbedBuilder\n" +
                              "docs SendMessageAsync in IMessageChannel\n" +
                              "docs search send message first method\n" +
                              "docs IGuildUser.Nickname";
                });
                await ReplyAsync("", embed: eb);
            }
            else
            {
                using (Context.Channel.EnterTypingState())
                {
                    try
                    {
                        await ReplyAsync(await Context.MainHandler.DocsHandler.Run(query));
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                    }
                }
            }
        }

        /* How it'll work:
         * howto get nickname with TYPE
         * howto send message with parent type
         */
        /*[Command("howto")]
        public async Task Howto([Remainder] string query = null)
        {
            if (query == null)
             throw new Exception("Usage: howto [query]");
            if (query == "help")
            {
                EmbedBuilder eb = new EmbedBuilder();
                eb.Title = "Howto Help";
                eb.Description = "Usage: howto [query]";
                eb.AddField((x) =>
                {
                    x.IsInline = false;
                    x.Name = "Keywords";
                    x.Value = "search, first, method, type, property, in";
                });
                eb.AddField((x) =>
                {
                    x.IsInline = false;
                    x.Name = "Examples";
                    x.Value = ".docs EmbedBuilder\n" +
                              ".docs SendMessageAsync in IMessageChannel\n" +
                              ".docs search send message first method\n" +
                              ".docs IGuildUser.Nickname";
                });
                await ReplyAsync("", embed: eb);
            }
            else
            {
                using (Context.Channel.EnterTypingState())
                {
                    try
                    {
                        //await ReplyAsync(await Context.MainHandler.DocsHandler.Run(query));
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                    }
                }
            }
        }*/

        /* How it'll work:
         * method Method
         * method Method in Namespace
         */
        [Command("method")]
        public async Task Method([Remainder] string query = null)
        {
            if (query == null)
                throw new Exception("Usage: method [query]");
            if (query == "help")
            {
                EmbedBuilder eb = new EmbedBuilder();
                eb.Title = "Method Help";
                eb.Description = "Usage: method [query]";
                eb.AddField((x) =>
                {
                    x.IsInline = false;
                    x.Name = "Keywords";
                    x.Value = "search, first, in";
                });
                eb.AddField((x) =>
                {
                    x.IsInline = false;
                    x.Name = "Examples";
                    x.Value = "method SendMessageAsync\n" +
                              "method GetChannelAsync in IGuild\n" +
                              "method search send message first\n" +
                              "method IGuildUser.ModifyAsync";
                });
                await ReplyAsync("", embed: eb);
            }
            else
            {
                try
                {
                    var tuple = Context.MainHandler.MethodHelperHandler.Run(query);
                    await ReplyAsync(tuple.Item1, false, tuple.Item2);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
        }

        [Command("clean")]
        public async Task Clean(int messages = 30)
        {
            var msgs = await Context.Channel.GetMessagesAsync(messages).Flatten();
            msgs = msgs.Where(x => x.Author.Id == Context.Client.CurrentUser.Id);
            foreach (IMessage msg in msgs)
                await msg.DeleteAsync();
        }

        [Command("source")]
        public async Task Source()
        {
            await ReplyAsync("Source: https://github.com/SubZero0/DiscordNet-Docs");
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
            await ReplyAsync("", embed: await Context.MainHandler.CommandHandler.HelpEmbedBuilder(Context, command));
        }
    }
}