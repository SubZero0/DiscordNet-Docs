using DiscordNet.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DiscordNet.MethodHelper
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
            bool takeFirst = false, isSearch = false;
            if (_text.IndexOf(" first ", StringComparison.OrdinalIgnoreCase) != -1)
            {
                takeFirst = true;
                _text = _text.Replace(" first ", "");
            }
            if (_text.IndexOf(" search ", StringComparison.OrdinalIgnoreCase) != -1)
            {
                isSearch = true;
                Regex rgx = new Regex("( search | find )");
                _text = rgx.Replace(_text, "");
            }
            string nspace = null;
            int idx;
            if ((idx = _text.IndexOf(" in ", StringComparison.OrdinalIgnoreCase)) != -1)
            {
                idx += 4;
                nspace = _text.Substring(idx);
                int idx2;
                if ((idx2 = nspace.IndexOf(' ')) != -1)
                    nspace = nspace.Substring(0, idx2);
                _text = _text.Replace($" in {nspace}", "");
            }
            if (_text.Contains(".") && idx == -1)
            {
                nspace = _text.Substring(0, _text.LastIndexOf('.'));
                int lidx;
                if ((lidx = nspace.LastIndexOf(' ')) != -1)
                    nspace = nspace.Substring(lidx);
                _text = _text.Replace($"{nspace}.", "");
            }
            _text = _text.Trim();
            if (nspace != null)
                nspace = nspace.Trim();
            return new InterpreterResult(_text, nspace, takeFirst, isSearch, false, true, false);
        }
    }
}
