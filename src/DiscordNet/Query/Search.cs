using DiscordNet.Query.Extensions;
using DiscordNet.Query.Results;
using DiscordNet.Query.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DiscordNet.Query
{
    public class Search
    {
        private InterpreterResult _result;
        private Cache _cache;
        public Search(InterpreterResult result, Cache cache)
        {
            _result = result;
            _cache = cache;
        }

        public SearchResult<object> Run()
        {
            List<object> found = new List<object>();
            if (_result.SearchTypes)
                found.AddRange(_cache.SearchTypes(_result.Text, !_result.IsSearch));
            if (_result.SearchMethods)
                found.AddRange(_cache.SearchMethods(_result.Text, !_result.IsSearch));
            if (_result.SearchProperties)
                found.AddRange(_cache.SearchProperties(_result.Text, !_result.IsSearch));
            if (_result.SearchEvents)
                found.AddRange(_cache.SearchEvents(_result.Text, !_result.IsSearch));
            found = NamespaceFilter(found);
            if (_result.TakeFirst && found.Count > 0)
            {
                var first = found.First();
                return new SearchResult<object>(found.Where(x => BaseDisplay.GetPath(x, false) == BaseDisplay.GetPath(first, false)).ToList());
            }
            return new SearchResult<object>(found);
        }

        internal List<object> NamespaceFilter(List<object> oldList)
        {
            List<object> list = new List<object>();
            foreach (object o in oldList)
            {
                if (o is TypeInfo)
                {
                    TypeInfo r = (TypeInfo)o;
                    if (!r.Namespace.StartsWith("Discord.API"))
                    {
                        if (_result.Namespace != null && r.Namespace.IndexOf(_result.Namespace, StringComparison.OrdinalIgnoreCase) != -1)
                            list.Add(o);
                        else if (_result.Namespace == null)
                            list.Add(o);
                    }
                }
                else if (o is MethodInfoWrapper)
                {
                    MethodInfoWrapper r = (MethodInfoWrapper)o;
                    if (!r.Parent.Namespace.StartsWith("Discord.API"))
                    {
                        if (_result.Namespace != null && $"{r.Parent.Namespace}.{r.Parent.Name}".IndexOf(_result.Namespace, StringComparison.OrdinalIgnoreCase) != -1)
                            list.Add(o);
                        else if (_result.Namespace == null)
                            list.Add(o);
                    }
                }
                else if (o is PropertyInfoWrapper)
                {
                    PropertyInfoWrapper r = (PropertyInfoWrapper)o;
                    if (!r.Parent.Namespace.StartsWith("Discord.API"))
                    {
                        if (_result.Namespace != null && $"{r.Parent.Namespace}.{r.Parent.Name}".IndexOf(_result.Namespace, StringComparison.OrdinalIgnoreCase) != -1)
                            list.Add(o);
                        else if (_result.Namespace == null)
                            list.Add(o);
                    }
                }
                else if (o is EventInfo)
                {
                    EventInfo r = (EventInfo)o;
                    if (!r.DeclaringType.Namespace.StartsWith("Discord.API"))
                    {
                        if (_result.Namespace != null && $"{r.DeclaringType.Namespace}.{r.DeclaringType.Name}".IndexOf(_result.Namespace, StringComparison.OrdinalIgnoreCase) != -1)
                            list.Add(o);
                        else if (_result.Namespace == null)
                            list.Add(o);
                    }
                }
            }
            return list;
        }
    }
}
