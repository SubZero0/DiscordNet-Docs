using DiscordNet.Query.Wrappers;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace DiscordNet.Github
{
    public class GithubRest
    {
        private const string ApiUrl = "https://api.github.com";
        private const string AcceptHeader = "application/vnd.github.v3+json";
        private const string UserAgentHeader = "Discord.Net Docs Bot/1.0";
        private const string AuthorizationHeader = "token ...";

        private static async Task<JObject> SendRequestAsync(HttpMethod method, string endpoint, string extra = null)
        {
            using (var http = new HttpClient())
            {
                var request = new HttpRequestMessage(method, $"{ApiUrl}{endpoint}{extra}");
                request.Headers.Add("Accept", AcceptHeader);
                request.Headers.Add("User-Agent", UserAgentHeader);
                request.Headers.Add("Authorization", AuthorizationHeader);
                var response = await http.SendAsync(request);
                if (response.IsSuccessStatusCode)
                    return JObject.Parse(await response.Content.ReadAsStringAsync());
                throw new Exception($"{response.ReasonPhrase}: {await response.Content.ReadAsStringAsync()}");
            }
        }

        public static async Task<IEnumerable<string>> GetIssuesUrlsAsync(IEnumerable<string> numbers)
        {
            var result = await Task.WhenAll(numbers.Select(x => SendRequestAsync(HttpMethod.Get, "/repos/RogueException/Discord.Net/issues/", x)));
            return result.Select(x => (string)x["html_url"]);
        }

        public static async Task<List<GitSearchResult>> SearchAsync(string search, string filename = null)
        {
            var extra = $"?q=repo:RogueException/Discord.Net+language:cs+in:file{(filename == null ? "" : $"+filename:{filename}")}+{search.Replace(' ', '+')}&per_page=100";
            var result = await SendRequestAsync(HttpMethod.Get, "/search/code", extra);
            var items = (JArray)result["items"];
            var list = new List<GitSearchResult>();
            foreach (var item in items)
                list.Add(new GitSearchResult { Name = (string)item["name"], HtmlUrl = (string)item["html_url"] });
            int totalCount = (int)result["total_count"];
            if (totalCount > 100)
            {
                int pages = (int)Math.Floor(totalCount / 100f);
                for (int i = 2; i <= pages + 1; i++)
                {
                    extra = $"?q=repo:RogueException/Discord.Net+language:cs+in:file{(filename == null ? "" : $"+filename:{filename}")}+{search.Replace(' ', '+')}&per_page=100&page={i}";
                    result = await SendRequestAsync(HttpMethod.Get, "/search/code", extra);
                    items = (JArray)result["items"];
                    foreach (var item in items)
                        list.Add(new GitSearchResult { Name = (string)item["name"], HtmlUrl = (string)item["html_url"] });
                }
            }
            return list;
        }

        public static async Task<string> GetTypeUrlAsync(TypeInfoWrapper type)
        {
            var search = await SearchAsync(type.Name, $"{type.Name}.cs");
            return search.FirstOrDefault(x => x.Name == $"{type.Name}.cs")?.HtmlUrl ?? search.FirstOrDefault()?.HtmlUrl ?? null; //null = Not found
        }

        public static async Task<string> GetEventUrlAsync(EventInfoWrapper ev)
        {
            var search = await SearchAsync(ev.Event.DeclaringType.Name, $"{ev.Event.DeclaringType.Name}.Events.cs");
            var result = search.FirstOrDefault(x => x.Name == $"{ev.Event.DeclaringType.Name}.Events.cs")?.HtmlUrl ?? search.FirstOrDefault()?.HtmlUrl;
            if (result != null)
            {
                using (var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) })
                {
                    string url = result.Replace("/blob/", "/raw/");
                    HttpResponseMessage httpResult = await client.GetAsync(url);
                    if (!httpResult.IsSuccessStatusCode)
                        return null;

                    var tree = CSharpSyntaxTree.ParseText(await httpResult.Content.ReadAsStringAsync());
                    var root = (CompilationUnitSyntax)tree.GetRoot();
                    var source = root.DescendantNodes().OfType<EventDeclarationSyntax>().FirstOrDefault(x => x.Identifier.ValueText == ev.Event.Name);
                    if (source == null)
                        return result;
                    var startLine = source.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                    var endLine = source.GetLocation().GetLineSpan().EndLinePosition.Line + 1;
                    return $"{result}{(startLine == endLine ? $"#L{startLine}" : $"#L{startLine}-L{endLine}")}";
                }
            }
            return result ?? await GetTypeUrlAsync(ev.Parent);
        }

        public static async Task<string> GetMethodUrlAsync(MethodInfoWrapper method)
        {
            var search = await SearchAsync(method.Method.Name, $"{method.Method.DeclaringType.Name}.cs");
            var result = search.FirstOrDefault(x => x.Name == $"{method.Method.DeclaringType.Name}.cs")?.HtmlUrl ?? search.FirstOrDefault()?.HtmlUrl;
            if (result != null)
            {
                using (var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) })
                {
                    string url = result.Replace("/blob/", "/raw/");
                    HttpResponseMessage httpResult = await client.GetAsync(url);
                    if (!httpResult.IsSuccessStatusCode)
                        return null;

                    var tree = CSharpSyntaxTree.ParseText(await httpResult.Content.ReadAsStringAsync());
                    var root = (CompilationUnitSyntax)tree.GetRoot();
                    var source = root.DescendantNodes().OfType<MethodDeclarationSyntax>().FirstOrDefault(x => x.Identifier.ValueText == method.Method.Name);
                    if (source == null)
                        return result;
                    var startLine = source.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                    var endLine = source.GetLocation().GetLineSpan().EndLinePosition.Line + 1;
                    return $"{result}{(startLine == endLine ? $"#L{startLine}" : $"#L{startLine}-L{endLine}")}";
                }
            }
            return await GetTypeUrlAsync(method.Parent);
        }

        public static async Task<string> GetPropertyUrlAsync(PropertyInfoWrapper property)
        {
            var search = await SearchAsync(property.Property.Name, $"{property.Property.DeclaringType.Name}.cs");
            var result = search.FirstOrDefault(x => x.Name == $"{property.Property.DeclaringType.Name}.cs")?.HtmlUrl ?? search.FirstOrDefault()?.HtmlUrl;
            if(result != null)
            {
                using (var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) })
                {
                    string url = result.Replace("/blob/", "/raw/");
                    HttpResponseMessage httpResult = await client.GetAsync(url);
                    if (!httpResult.IsSuccessStatusCode)
                        return null;

                    var tree = CSharpSyntaxTree.ParseText(await httpResult.Content.ReadAsStringAsync());
                    var root = (CompilationUnitSyntax)tree.GetRoot();
                    var source = root.DescendantNodes().OfType<PropertyDeclarationSyntax>().FirstOrDefault(x => x.Identifier.ValueText == property.Property.Name);
                    if (source == null)
                        return result;
                    var startLine = source.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                    var endLine = source.GetLocation().GetLineSpan().EndLinePosition.Line + 1;
                    return $"{result}{(startLine == endLine ? $"#L{startLine}" : $"#L{startLine}-L{endLine}")}";
                }
            }
            return await GetTypeUrlAsync(property.Parent);
        }
    }

    public class GitSearchResult
    {
        public string Name { get; protected internal set; }
        public string HtmlUrl { get; protected internal set; }

        public override string ToString()
        {
            return $"{Name}: {HtmlUrl}";
        }
    }
}
