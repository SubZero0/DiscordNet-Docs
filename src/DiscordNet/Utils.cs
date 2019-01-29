using System;
using System.Collections.Generic;
using System.Linq;

namespace DiscordNet
{
    public static class Utils
    {
        public static string ResolveHtml(string html)
        {
            //TODO: Pass summary, replace \n's with spaces, replace </p>'s with \n's, strip tags, decode html
            //ISSUE: <p></p> could be empty, check for \n on first char
            return "";
        }

        public static IEnumerable<T> RandomShuffle<T>(this IEnumerable<T> source)
            => source.Select(t => new {Index = Guid.NewGuid(), Value = t}).OrderBy(p => p.Index).Select(p => p.Value);

        public static string BuildType(Type type)
        {
            string typeName = type.Name, typeGeneric = "";
            int idx;
            if ((idx = typeName.IndexOf('`')) != -1)
            {
                typeName = typeName.Substring(0, idx);
                var generics = type.GetGenericArguments();
                if (generics.Any())
                    typeGeneric = string.Join(", ", generics.Select(x => BuildType(x)));
            }
            return GetTypeName(type, typeName, typeGeneric);
        }

        private static string GetTypeName(Type type, string name, string generic)
        {
            if (Nullable.GetUnderlyingType(type) != null)
                return $"{generic}?";
            if (type.IsByRef)
                return BuildType(type.GetElementType());
            return Aliases.ContainsKey(type) ? Aliases[type] : $"{name}{(string.IsNullOrEmpty(generic) ? "" : $"<{generic}>")}";
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
