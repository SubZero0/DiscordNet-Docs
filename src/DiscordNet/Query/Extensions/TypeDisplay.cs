using Discord;
using DiscordNet.Query.Results;
using DiscordNet.Query.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace DiscordNet.Query
{
    public partial class ResultDisplay
    {
        private async Task<EmbedBuilder> ShowTypesAsync(EmbedBuilder eb, EmbedAuthorBuilder eab, IEnumerable<TypeInfoWrapper> list)
        {
            TypeInfoWrapper first = list.First();
            DocsHttpResult result;
            string pageUrl = SanitizeDocsUrl($"{first.TypeInfo.Namespace}.{first.TypeInfo.Name}");
            try
            {
                result = await GetWebDocsAsync($"https://discord.foxbot.me/docs/api/{pageUrl}.html", first);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                result = new DocsHttpResult($"https://discord.foxbot.me/docs/api/{pageUrl}.html");
            }
            eab.Name = $"{(first.TypeInfo.IsInterface ? "Interface" : (first.TypeInfo.IsEnum ? "Enum" : "Type"))}: {first.TypeInfo.Namespace}.{first.DisplayName}";
            eab.Url = result.Url;//$"https://discord.foxbot.me/docs/api/{first.Namespace}.{first.Name}.html";
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
            CacheBag cb = _cache.GetCacheBag(first);
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
                    x.Value = String.Join("\n", methods.Values.Select(y => $"``{i++}-``{(IsInherited(new MethodInfoWrapper(first, y)) ? " (i)" : "")} {y.Name}(...)"));
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
                    x.Value = String.Join("\n", properties.Values.Select(y => $"``{i++}-``{(IsInherited(new PropertyInfoWrapper(first, y)) ? " (i)" : "")} {y.Name}"));
                });
            }
            if (first.TypeInfo.IsEnum)
            {
                var enumValues = first.TypeInfo.GetEnumNames();
                int now = 0, max = (3 < enumValues.Length ? 3 : enumValues.Length);
                Random r = new Random();
                List<int> fields = new List<int>();
                while (now != max)
                {
                    int n = r.Next(enumValues.Length);
                    while (fields.Contains(n))
                    {
                        n++;
                        if (n == enumValues.Length)
                            n = 0;
                    }
                    fields.Add(n);
                    now++;
                }
                int i = 1;
                eb.AddField((x) =>
                {
                    x.IsInline = true;
                    x.Name = $"Some fields ({max}/{enumValues.Length}):";
                    x.Value = String.Join("\n", fields.Select(y => $"``{i}-`` {enumValues[fields[(i++)-1]]}"));
                });
            }
            return eb;
        }
    }
}
