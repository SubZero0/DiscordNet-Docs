using System.Reflection;

namespace DiscordNet.Query
{
    public class MethodInfoWrapper : BaseInfoWrapper
    {
        public MethodInfo Method { get; private set; }
        public TypeInfoWrapper Parent { get; private set; }
        public string Namespace { get { return Parent.Namespace; } }

        public MethodInfoWrapper(TypeInfoWrapper parent, MethodInfo method)
        {
            Parent = parent;
            Method = method;
        }
    }
}
