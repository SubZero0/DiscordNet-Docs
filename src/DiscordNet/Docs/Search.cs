using DiscordNet.Extensions;
using DiscordNet.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace DiscordNet.Docs
{
    public class Search : SearchExtension
    {
        private InterpreterResult _result;
        public Search(InterpreterResult result)
        {
            _result = result;
        }

        public SearchResult<object> Run()
        {
            List<object> found = new List<object>();
            if (_result.SearchTypes)
                found.AddRange(FindType(_result));
            if (_result.SearchMethods)
                found.AddRange(FindMethod(_result));
            if (_result.SearchProperties)
                found.AddRange(FindProperty(_result));
            found = NamespaceFilter(found);
            if (_result.TakeFirst && found.Count > 0)
                return new SearchResult<object>(new List<object> { found.First() });
            return new SearchResult<object>(found.GroupBy(x => GetPath(x)).Select(x => x.First()).ToList());
        }

        internal string GetPath(object o)
        {
            if (o is TypeInfo)
            {
                TypeInfo r = (TypeInfo)o;
                return $"Type: {r.Name} in {r.Namespace}";
            }
            if (o is MethodInfo)
            {
                MethodInfo r = (MethodInfo)o;
                return $"Method: {r.Name} in {r.DeclaringType.Namespace}.{r.DeclaringType.Name}";
            }
            if (o is PropertyInfo)
            {
                PropertyInfo r = (PropertyInfo)o;
                return $"Property: {r.Name} in {r.DeclaringType.Namespace}.{r.DeclaringType.Name}";
            }
            return o.GetType().ToString();
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
                if (o is MethodInfo)
                {
                    MethodInfo r = (MethodInfo)o;
                    if (!r.DeclaringType.Namespace.StartsWith("Discord.API"))
                    {
                        if (_result.Namespace != null && $"{r.DeclaringType.Namespace}.{r.DeclaringType.Name}".IndexOf(_result.Namespace, StringComparison.OrdinalIgnoreCase) != -1)
                            list.Add(o);
                        else if (_result.Namespace == null)
                            list.Add(o);
                    }
                }
                if (o is PropertyInfo)
                {
                    PropertyInfo r = (PropertyInfo)o;
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
