using DiscordNet.Query.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DiscordNet.Query
{
    /*      Notes:
     * Interfaces dont get their inherited members...
     * It's not possible to find SendMessageAsync inside ITextChannel
     * Maybe the Search would be better if it started looking first the namespace if provided?
     * 
     *      Add a new query type:
     * howto get nickname with TYPE
     * howto send message with parent type
     */
    public class Search
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
                found.AddRange(FindType());
            if (_result.SearchMethods)
                found.AddRange(FindMethod());
            if (_result.SearchProperties)
                found.AddRange(FindProperty());
            found = NamespaceFilter(found);
            if (_result.TakeFirst && found.Count > 0)
            {
                var first = found.First();
                return new SearchResult<object>(found.Where(x => GetPath(x) == GetPath(first)).ToList());
            }
            return new SearchResult<object>(found);
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

        internal List<TypeInfo> FindType()
        {
            List<TypeInfo> list = new List<TypeInfo>();
            foreach (var a in Assembly.GetEntryAssembly().GetReferencedAssemblies())
                if (a.Name.StartsWith("Discord"))
                {
                    Assembly loaded = Assembly.Load(a);
                    var found = loaded.GetExportedTypes().Where(x => (_result.IsSearch ? SearchFunction(x.Name) : x.Name.ToLower() == _result.Text.ToLower())).Select(x => x.GetTypeInfo());
                    if (found.Count() != 0)
                        list.AddRange(found);
                    found = loaded.GetExportedTypes().SelectMany(x => x.GetInterfaces()).Where(x => (_result.IsSearch ? SearchFunction(x.Name) : x.Name.ToLower() == _result.Text.ToLower())).Select(x => x.GetTypeInfo());
                    if (found.Count() != 0)
                        list.AddRange(found);
                }
            return list;
        }

        internal List<MethodInfo> FindMethod()
        {
            List<MethodInfo> list = new List<MethodInfo>();
            foreach (var a in Assembly.GetEntryAssembly().GetReferencedAssemblies())
                if (a.Name.StartsWith("Discord"))
                {
                    Assembly loaded = Assembly.Load(a);
                    foreach (Type t in loaded.GetExportedTypes())
                    {
                        var found = t.GetMethods().Where(x => (_result.IsSearch ? SearchFunction(x.Name) : x.Name.ToLower() == _result.Text.ToLower()) && x.IsPublic && !x.IsSpecialName);
                        if (found.Count() != 0)
                            list.AddRange(found);
                        found = t.GetInterfaces().SelectMany(x => x.GetMethods()).Where(x => (_result.IsSearch ? SearchFunction(x.Name) : x.Name.ToLower() == _result.Text.ToLower()) && x.IsPublic && !x.IsSpecialName);
                        if (found.Count() != 0)
                            list.AddRange(found);
                    }
                }
            return list;
        }

        internal List<PropertyInfo> FindProperty()
        {
            List<PropertyInfo> list = new List<PropertyInfo>();
            foreach (var a in Assembly.GetEntryAssembly().GetReferencedAssemblies())
                if (a.Name.StartsWith("Discord"))
                {
                    Assembly loaded = Assembly.Load(a);
                    foreach (Type t in loaded.GetExportedTypes())
                    {
                        var found = t.GetProperties().Where(x => (_result.IsSearch ? SearchFunction(x.Name) : x.Name.ToLower() == _result.Text.ToLower()));
                        if (found.Count() != 0)
                            list.AddRange(found);
                        found = t.GetInterfaces().SelectMany(x => x.GetProperties()).Where(x => (_result.IsSearch ? SearchFunction(x.Name) : x.Name.ToLower() == _result.Text.ToLower()));
                        if (found.Count() != 0)
                            list.AddRange(found);
                    }
                }
            return list;
        }

        internal bool SearchFunction(string objectName)
        {
            foreach (string s in _result.Text.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
                if (objectName.IndexOf(s, StringComparison.OrdinalIgnoreCase) == -1)
                    return false;
            return true;
        }
    }
}
