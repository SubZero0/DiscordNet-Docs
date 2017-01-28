using DiscordNet.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DiscordNet.Extensions
{
    /*      Notes:
     * Interfaces dont get their inherited members...
     * It's not possible to find SendMessageAsync inside ITextChannel
     * Maybe the Search would be better if it started looking first the namespace if provided?
     */
    public class SearchExtension
    {
        internal List<TypeInfo> FindType(InterpreterResult _result)
        {
            List<TypeInfo> list = new List<TypeInfo>();
            foreach (var a in Assembly.GetEntryAssembly().GetReferencedAssemblies())
                if (a.Name.StartsWith("Discord"))
                {
                    Assembly loaded = Assembly.Load(a);
                    var found = loaded.GetExportedTypes().Where(x => (_result.IsSearch ? SearchFunction(x.Name, _result) : x.Name.ToLower() == _result.Text.ToLower())).Select(x => x.GetTypeInfo());
                    if (found.Count() != 0)
                        list.AddRange(found);
                    found = loaded.GetExportedTypes().SelectMany(x => x.GetInterfaces()).Where(x => (_result.IsSearch ? SearchFunction(x.Name, _result) : x.Name.ToLower() == _result.Text.ToLower())).Select(x => x.GetTypeInfo());
                    if (found.Count() != 0)
                        list.AddRange(found);
                }
            return list;
        }

        internal List<MethodInfo> FindMethod(InterpreterResult _result)
        {
            List<MethodInfo> list = new List<MethodInfo>();
            foreach (var a in Assembly.GetEntryAssembly().GetReferencedAssemblies())
                if (a.Name.StartsWith("Discord"))
                {
                    Assembly loaded = Assembly.Load(a);
                    foreach (Type t in loaded.GetExportedTypes())
                    {
                        var found = t.GetMethods().Where(x => (_result.IsSearch ? SearchFunction(x.Name, _result) : x.Name.ToLower() == _result.Text.ToLower()) && x.IsPublic && !x.IsSpecialName);
                        if (found.Count() != 0)
                            list.AddRange(found);
                        found = t.GetInterfaces().SelectMany(x => x.GetMethods()).Where(x => (_result.IsSearch ? SearchFunction(x.Name, _result) : x.Name.ToLower() == _result.Text.ToLower()) && x.IsPublic && !x.IsSpecialName);
                        if (found.Count() != 0)
                            list.AddRange(found);
                    }
                }
            return list;
        }

        internal List<PropertyInfo> FindProperty(InterpreterResult _result)
        {
            List<PropertyInfo> list = new List<PropertyInfo>();
            foreach (var a in Assembly.GetEntryAssembly().GetReferencedAssemblies())
                if (a.Name.StartsWith("Discord"))
                {
                    Assembly loaded = Assembly.Load(a);
                    foreach (Type t in loaded.GetExportedTypes())
                    {
                        var found = t.GetProperties().Where(x => (_result.IsSearch ? SearchFunction(x.Name, _result) : x.Name.ToLower() == _result.Text.ToLower()));
                        if (found.Count() != 0)
                            list.AddRange(found);
                        found = t.GetInterfaces().SelectMany(x => x.GetProperties()).Where(x => (_result.IsSearch ? SearchFunction(x.Name, _result) : x.Name.ToLower() == _result.Text.ToLower()));
                        if (found.Count() != 0)
                            list.AddRange(found);
                    }
                }
            return list;
        }

        internal bool SearchFunction(string objectName, InterpreterResult _result)
        {
            foreach (string s in _result.Text.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
                if (objectName.IndexOf(s, StringComparison.OrdinalIgnoreCase) == -1)
                    return false;
            return true;
        }
    }
}
