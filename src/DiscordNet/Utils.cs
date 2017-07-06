using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DiscordNet
{
    public static class Utils
    {
        public static IEnumerable<T> RandomShuffle<T>(this IEnumerable<T> source)
        {
            return source.Select(t => new {Index = Guid.NewGuid(), Value = t}).OrderBy(p => p.Index).Select(p => p.Value);
        }

        public static string BuildType(Type type)
        {
            string typeName = type.Name, typeGeneric = "";
            int idx;
            if ((idx = typeName.IndexOf('`')) != -1)
            {
                typeName = typeName.Substring(0, idx);
                var generics = type.GetGenericArguments();
                if (generics.Any())
                    typeGeneric = $"<{String.Join(", ", generics.Select(x => BuildType(x)))}>";
            }
            return GetTypeName(type, typeName, typeGeneric);
        }

        private static string GetTypeName(Type type, string name, string generic)
        {
            if (type.IsInstanceOfType(typeof(Nullable<>)))
                return $"{generic}?";
            return Aliases.ContainsKey(type) ? Aliases[type] : $"{name}{generic}";
        }

        private static readonly Dictionary<Type, string> Aliases = new Dictionary<Type, string>()
        {
            { typeof(byte), "byte" },
            { typeof(sbyte), "sbyte" },
            { typeof(short), "short" },
            { typeof(ushort), "ushort" },
            { typeof(int), "int" },
            { typeof(uint), "uint" },
            { typeof(long), "long" },
            { typeof(ulong), "ulong" },
            { typeof(float), "float" },
            { typeof(double), "double" },
            { typeof(decimal), "decimal" },
            { typeof(object), "object" },
            { typeof(bool), "bool" },
            { typeof(char), "char" },
            { typeof(string), "string" },
            { typeof(void), "void" }
        };
    }
}
