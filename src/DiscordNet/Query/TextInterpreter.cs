using DiscordNet.Query.Results;
using System;
using System.Text.RegularExpressions;

namespace DiscordNet.Query
{
    public class TextInterpreter
    {
        private string _text;
        public TextInterpreter(string text)
        {
            _text = $" {text} ";
        }

        public InterpreterResult Run()
        {
            //TODO: Better text parsing
            bool searchTypes = true, searchMethods = true, searchProperties = true, searchEvents = true, takeFirst = false;
            SearchType search = SearchType.NONE;
            if (_text.IndexOf(" type ", StringComparison.OrdinalIgnoreCase) != -1 || _text.IndexOf(" method ", StringComparison.OrdinalIgnoreCase) != -1 || _text.IndexOf(" property ", StringComparison.OrdinalIgnoreCase) != -1 || _text.IndexOf(" event ", StringComparison.OrdinalIgnoreCase) != -1)
            {
                if (_text.IndexOf(" type ", StringComparison.OrdinalIgnoreCase) == -1)
                    searchTypes = false;
                if (_text.IndexOf(" method ", StringComparison.OrdinalIgnoreCase) == -1)
                    searchMethods = false;
                if (_text.IndexOf(" property ", StringComparison.OrdinalIgnoreCase) == -1)
                    searchProperties = false;
                if (_text.IndexOf(" event ", StringComparison.OrdinalIgnoreCase) == -1)
                    searchEvents = false;
                Regex rgx = new Regex("( property | method | type | event )", RegexOptions.IgnoreCase);
                _text = rgx.Replace(_text, " ");
            }
            if (_text.IndexOf(" first ", StringComparison.OrdinalIgnoreCase) != -1)
            {
                takeFirst = true;
                Regex rgx = new Regex("( first )", RegexOptions.IgnoreCase);
                _text = rgx.Replace(_text, " ");
            }
            if (_text.IndexOf(" search ", StringComparison.OrdinalIgnoreCase) != -1)
            {
                search = SearchType.JUST_TEXT;
                Regex rgx = new Regex("( search )", RegexOptions.IgnoreCase);
                _text = rgx.Replace(_text, " ");
            }
            string nspace = null;
            int idx;
            if ((idx = _text.IndexOf(" in ", StringComparison.OrdinalIgnoreCase)) != -1)
            {
                if (search == SearchType.JUST_TEXT)
                    search = SearchType.ALL;
                else
                    search = SearchType.JUST_NAMESPACE;
                idx += 4;
                nspace = _text.Substring(idx);
                int idx2;
                if ((idx2 = nspace.IndexOf(' ')) != -1)
                    nspace = nspace.Substring(0, idx2);
                _text = _text.Replace($" in {nspace}", " ");
            }
            if (_text.Contains(".") && idx == -1)
            {
                if (search == SearchType.ALL)
                    search = SearchType.JUST_TEXT;
                else if (search == SearchType.JUST_NAMESPACE)
                    search = SearchType.NONE;
                nspace = _text.Substring(0, _text.LastIndexOf('.'));
                int lidx;
                if ((lidx = nspace.LastIndexOf(' ')) != -1)
                    nspace = nspace.Substring(lidx);
                _text = _text.Replace($"{nspace}.", "");
            }
            _text = _text.Trim();
            if (_text == "")
                new InterpreterResult("No text to search.");
            if (nspace != null)
                nspace = nspace.Trim();
            return new InterpreterResult(_text, nspace, takeFirst, search, searchTypes, searchMethods, searchProperties, searchEvents);
        }
    }
}
