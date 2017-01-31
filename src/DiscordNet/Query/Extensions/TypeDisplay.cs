using Discord;
using DiscordNet.Query.Results;
using DiscordNet.Query.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace DiscordNet.Query.Extensions
{
    public static class TypeDisplay
    {
        public static async Task<EmbedBuilder> ShowTypesAsync(this ResultDisplay rDisplay, EmbedBuilder eb, EmbedAuthorBuilder eab, IEnumerable<TypeInfo> list)
        {
            TypeInfo first = list.First();
            DocsHttpResult result;
            try
            {
                result = await BaseDisplay.GetWebDocsAsync($"https://discord.foxbot.me/docs/api/{first.Namespace}.{first.Name}.html", first);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                result = new DocsHttpResult($"https://discord.foxbot.me/docs/api/{first.Namespace}.{first.Name}.html");
            }
            eab.Name = $"{(first.IsInterface ? "Interface" : "Type")}: {first.Namespace}.{first.Name}";
            eab.Url = $"https://discord.foxbot.me/docs/api/{first.Namespace}.{first.Name}.html"; //or result.Url
            eb.AddField((x) =>
            {
                x.IsInline = false;
                x.Name = "Docs:";
                x.Value = eab.Url;
            });
            if (result.Summary != null)
                eb.AddField((x) =>
                {
                    x.IsInline = false;
                    x.Name = "Summary:";
                    x.Value = result.Summary;
                });
            if (result.Example != null)
                eb.AddField((x) =>
                {
                    x.IsInline = false;
                    x.Name = "Example:";
                    x.Value = result.Example;
                });
            CacheBag cb = rDisplay._cache.GetCacheBag(first);
            if (cb.Methods.Count != 0)
            {
                int now = 0, max = (3 < cb.Methods.Count ? 3 : cb.Methods.Count);
                Random r = new Random();
                Dictionary<int, MethodInfo> methods = new Dictionary<int, MethodInfo>();
                while (now != max)
                {
                    int n = r.Next(cb.Methods.Count);
                    while (methods.ContainsKey(n))
                    {
                        n++;
                        if (n == cb.Methods.Count)
                            n = 0;
                    }
                    methods[n] = cb.Methods.ElementAt(n);
                    now++;
                }
                int i = 1;
                eb.AddField((x) =>
                {
                    x.IsInline = true;
                    x.Name = $"Some methods ({max}/{cb.Methods.Count}):";
                    x.Value = String.Join("\n", methods.Values.Select(y => $"``{i++}-``{(BaseDisplay.IsInherited(new MethodInfoWrapper(first, y)) ? " (i)" : "")} {y.Name}(...)"));
                });
            }
            if (cb.Properties.Count != 0)
            {
                int now = 0, max = (3 < cb.Properties.Count ? 3 : cb.Properties.Count);
                Random r = new Random();
                Dictionary<int, PropertyInfo> properties = new Dictionary<int, PropertyInfo>();
                while (now != max)
                {
                    int n = r.Next(cb.Properties.Count);
                    while (properties.ContainsKey(n))
                    {
                        n++;
                        if (n == cb.Properties.Count)
                            n = 0;
                    }
                    properties[n] = cb.Properties.ElementAt(n);
                    now++;
                }
                int i = 1;
                eb.AddField((x) =>
                {
                    x.IsInline = true;
                    x.Name = $"Some properties ({max}/{cb.Properties.Count}):";
                    x.Value = String.Join("\n", properties.Values.Select(y => $"``{i++}-``{(BaseDisplay.IsInherited(new PropertyInfoWrapper(first, y)) ? " (i)" : "")} {y.Name}"));
                });
            }
            return eb;
        }
    }
}
