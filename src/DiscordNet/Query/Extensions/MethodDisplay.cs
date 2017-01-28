using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace DiscordNet.Query.Extensions
{
    public class MethodDisplay
    {
        internal string ShowMethods(IEnumerable<MethodInfo> list)
        {
            string result;
            MethodInfo first = list.First();
            int i = 1;
            var overloads = list.GroupBy((x) => BuildMethod(x)); //TODO: Duplicates... probably coming from the interfaces?
            result = $"**Overloads:**\n{String.Join("\n", overloads.Select(x => $"``{i++}-`` {x.Key}"))}";
            var tips = Tips(overloads.Select(x => x.First()));
            i = 1;
            result += $"{(tips.Count == 0 ? "" : $"\n\n**Tips & examples:**\n{String.Join("\n", tips.Select(x => $"``{i++}-`` {x}"))}")}";
            return result;
        }

        internal List<string> Tips(IEnumerable<MethodInfo> list)
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
            if (type.IsConstructedGenericType)
                generic = $"<{String.Join(", ", type.GetGenericArguments().Select(x => BuildType(x)))}>";
            return $"{name}{generic}";
        }
    }
}
