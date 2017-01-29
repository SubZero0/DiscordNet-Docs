using System.Reflection;

namespace DiscordNet.Query.Wrappers
{
    public class MethodInfoWrapper
    {
        public MethodInfo Method { get; private set; }
        public TypeInfo Parent { get; private set; }
        public MethodInfoWrapper(TypeInfo parent, MethodInfo method)
        {
            Parent = parent;
            Method = method;
        }
    }
}
