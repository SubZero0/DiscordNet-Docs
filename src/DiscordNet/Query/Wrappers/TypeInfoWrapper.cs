using System;
using System.Reflection;

namespace DiscordNet.Query.Wrappers
{
    public class TypeInfoWrapper
    {
        public TypeInfo TypeInfo { get; private set; }
        public string DisplayName { get; private set; }
        public TypeInfoWrapper(TypeInfo typeInfo)
        {
            TypeInfo = typeInfo;
            DisplayName = typeInfo.Name;
            int idx;
            if ((idx = DisplayName.IndexOf('`')) != -1)
                DisplayName = $"{DisplayName.Substring(0, idx)}<{typeInfo.GetGenericArguments()[0]}>"; //TODO: Only valid with one generic argument, could be more
        }
    }
}
