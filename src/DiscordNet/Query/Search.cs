using DiscordNet.Query.Results;
using DiscordNet.Query.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

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
            bool searchText = _result.Search == SearchType.ALL || _result.Search == SearchType.JUST_TEXT;
            if (_result.SearchTypes)
                found.AddRange(_cache.SearchTypes(_result.Text, !searchText));
            if (_result.SearchMethods)
                found.AddRange(_cache.SearchMethods(_result.Text, !searchText));
            if (_result.SearchProperties)
                found.AddRange(_cache.SearchProperties(_result.Text, !searchText));
            if (_result.SearchEvents)
                found.AddRange(_cache.SearchEvents(_result.Text, !searchText));
            found = NamespaceFilter(found, _result.Search == SearchType.NONE || _result.Search == SearchType.JUST_TEXT);
            if (_result.TakeFirst && found.Count > 0)
            {
                var first = found.First();
                return new SearchResult<object>(found.Where(x => ResultDisplay.GetPath(x, false) == ResultDisplay.GetPath(first, false)).ToList());
            }
            return new SearchResult<object>(found);
        }

        private List<object> NamespaceFilter(List<object> oldList, bool exactName = true)
        {
            List<object> list = new List<object>();
            foreach (object o in oldList)
            {
                if (o is TypeInfoWrapper)
                {
                    TypeInfoWrapper r = (TypeInfoWrapper)o;
                    if (!r.TypeInfo.Namespace.StartsWith("Discord.API") && CompareNamespaces(r.TypeInfo.Namespace))
                        list.Add(o);
                }
                else if (o is MethodInfoWrapper)
                {
                    MethodInfoWrapper r = (MethodInfoWrapper)o;
                    if (!r.Parent.TypeInfo.Namespace.StartsWith("Discord.API") && CompareNamespaces(r.Parent.TypeInfo))
                        list.Add(o);
                }
                else if (o is PropertyInfoWrapper)
                {
                    PropertyInfoWrapper r = (PropertyInfoWrapper)o;
                    if (!r.Parent.TypeInfo.Namespace.StartsWith("Discord.API") && CompareNamespaces(r.Parent.TypeInfo))
                        list.Add(o);
                }
                else if (o is EventInfoWrapper)
                {
                    EventInfoWrapper r = (EventInfoWrapper)o;
                    if (!r.Parent.TypeInfo.Namespace.StartsWith("Discord.API") && CompareNamespaces(r.Parent.TypeInfo))
                        list.Add(o);
                }
            }
            return list;
        }

        private bool CompareNamespaces(TypeInfo toCompare) => CompareNamespaces($"{toCompare.Namespace}.{toCompare.Name}");
        private bool CompareNamespaces(string toCompare)
        {
            if (_result.Namespace == null)
                return true;
            if (_result.Search == SearchType.ALL || _result.Search == SearchType.JUST_NAMESPACE)
                return toCompare.IndexOf(_result.Namespace, StringComparison.OrdinalIgnoreCase) != -1;
            Regex rgx = new Regex($"(\\.{_result.Namespace}\\b|\\b{_result.Namespace}\\.|\\b{_result.Namespace}\\b)", RegexOptions.IgnoreCase);
            return rgx.IsMatch(toCompare);
        }
    }
}
