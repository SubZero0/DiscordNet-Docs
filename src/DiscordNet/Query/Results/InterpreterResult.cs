namespace DiscordNet.Query.Results
{
    public class InterpreterResult
    {
        public string Text { get; private set; }
        public string Namespace { get; private set; }
        public bool SearchTypes { get; private set; }
        public bool SearchMethods { get; private set; }
        public bool SearchProperties { get; private set; }
        public bool TakeFirst { get; private set; }
        public bool IsSearch { get; private set; }
        public InterpreterResult(string text, string nspace = null, bool takeFirst = false, bool isSearch = false, bool searchTypes = true, bool searchMethods = true, bool searchProperties = true)
        {
            Text = text;
            Namespace = nspace;
            SearchTypes = searchTypes;
            SearchMethods = searchMethods;
            SearchProperties = searchProperties;
            TakeFirst = takeFirst;
            IsSearch = isSearch;
        }
    }
}
