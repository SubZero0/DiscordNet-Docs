using Discord;
using DiscordNet.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DiscordNet.MethodHelper
{
    public class ResultDisplay
    {
        private SearchResult<MethodInfo> _result;
        public ResultDisplay(SearchResult<MethodInfo> result)
        {
            _result = result;
        }

        public Tuple<string, EmbedBuilder> Run()
        {
            var groups = _result.List.GroupBy(x => $"{x.DeclaringType.Namespace}{x.DeclaringType.Name}{x.Name}");
            if(groups.Count() == 1)
                return Tuple.Create("", Show(_result.List));
            else
                return Tuple.Create(ShowMultiple(groups), (EmbedBuilder)null);
        }

        internal EmbedBuilder Show(List<MethodInfo> list)
        {
            MethodInfo first = list.First();
            EmbedAuthorBuilder eab = new EmbedAuthorBuilder();
            eab.IconUrl = "http://i.imgur.com/XW4RU5e.png";
            eab.Name = $"{first.DeclaringType.Namespace}.{first.DeclaringType.Name}.{first.Name}";
            //eab.Url = "#"; //Link to docs?
            EmbedBuilder eb = new EmbedBuilder();
            eb.Author = eab;
            int i = 1;
            eb.Description = $"**Overloads:**\n{String.Join("\n", list.Select(x => $"``{i++}-`` {BuildMethod(x)}"))}";
            var tips = Tips(list);
            i = 1;
            eb.Description += $"{(tips.Count == 0 ? "" : $"\n\n**Tips & examples:**\n{String.Join("\n", tips.Select(x => $"``{i++}-`` {x}"))}")}";
            return eb;
        }

        internal List<string> Tips(List<MethodInfo> list)
        {
            List<string> tips = new List<string>();
            if (list.FirstOrDefault(x => CompareTypes(x.ReturnType, typeof(Action)) || x.GetParameters().FirstOrDefault(y => CompareTypes(y.ParameterType, typeof(Action))) != null) != null)
                tips.Add("Action [only execute]: ``(x) => x.Something``");
            if (list.FirstOrDefault(x => CompareTypes(x.ReturnType, typeof(Func<>)) || x.GetParameters().FirstOrDefault(y => CompareTypes(y.ParameterType, typeof(Func<>))) != null) != null)
                tips.Add("Func [execute and return]: ``(x) => x.Something``");
            if (list.FirstOrDefault(x => CompareTypes(x.ReturnType, typeof(Task)) || x.GetParameters().FirstOrDefault(y => CompareTypes(y.ParameterType, typeof(Task))) != null) != null)
                tips.Add("Task: always remember to ``await`` if needed.");
            return tips;
        }

        internal bool CompareTypes(Type t1, Type t2)
        {
            return t1.FullName.StartsWith(t2.FullName);
        }

        internal string BuildMethod(MethodInfo mi)
        {
            return $"{BuildType(mi.ReturnType)} {mi.Name}({String.Join(", ", mi.GetParameters().Select(x => $"{BuildType(x.ParameterType)} {x.Name}{ParameterDefaultValue(x)}"))})";
        }

        internal string ParameterDefaultValue(ParameterInfo pi)
        {
            if (pi.HasDefaultValue)
                return $" = {pi.DefaultValue?.ToString() ?? "null"}";
            return "";
        }

        internal string BuildType(Type type)
        {
            string name = type.Name;
            int idx;
            if ((idx = name.IndexOf('`')) != -1)
                name = name.Substring(0, idx);
            string generic = "";
            if(type.IsConstructedGenericType)
                generic = $"<{String.Join(", ", type.GetGenericArguments().Select(x => BuildType(x)))}>";
            return $"{name}{generic}";
        }

        internal string ShowMultiple(IEnumerable<IGrouping<string, MethodInfo>> list)
        {
            if (list.Count() > 10)
                return $"**Too many results, try filtering your search. Some results:**\n{String.Join("\n", GetPaths(list.Take(10)))}";
            return $"**Did you mean:**\n{String.Join("\n", GetPaths(list))}\nTry looking at: ``method help`` to filter more your query.";
        }

        internal List<string> GetPaths(IEnumerable<IGrouping<string, MethodInfo>> list)
        {
            List<string> newlist = new List<string>();
            foreach (var group in list)
            {
                MethodInfo mi = group.First();
                newlist.Add($"{mi.Name}(...) in {mi.DeclaringType.Namespace}.{mi.DeclaringType.Name}");
            }
            return newlist;
        }
    }
}
