using DiscordNet.Extensions;
using DiscordNet.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace DiscordNet.MethodHelper
{
    public class Search : SearchExtension
    {
        private InterpreterResult _result;
        public Search(InterpreterResult result)
        {
            _result = result;
        }

        public SearchResult<MethodInfo> Run()
        {
            List<MethodInfo> found = new List<MethodInfo>();
            found.AddRange(FindMethod(_result));
            found = NamespaceFilter(found);
            if (_result.TakeFirst && found.Count > 0)
                return new SearchResult<MethodInfo>(found.Where(x => x.Name == found.First().Name).ToList());
            return new SearchResult<MethodInfo>(found);
        }

        internal List<MethodInfo> NamespaceFilter(List<MethodInfo> oldList)
        {
            List<MethodInfo> list = new List<MethodInfo>();
            foreach (MethodInfo mi in oldList)
                if (!mi.DeclaringType.Namespace.StartsWith("Discord.API"))
                {
                    if (_result.Namespace != null && $"{mi.DeclaringType.Namespace}.{mi.DeclaringType.Name}".IndexOf(_result.Namespace, StringComparison.OrdinalIgnoreCase) != -1)
                        list.Add(mi);
                    else if (_result.Namespace == null)
                        list.Add(mi);
                }
            return list;
        }
    }
}
