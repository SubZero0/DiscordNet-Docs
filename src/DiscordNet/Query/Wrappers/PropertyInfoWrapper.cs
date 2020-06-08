using System.Reflection;

namespace DiscordNet.Query
{
    public class PropertyInfoWrapper : BaseInfoWrapper
    {
        public PropertyInfo Property { get; private set; }
        public TypeInfoWrapper Parent { get; private set; }
        public string Namespace { get { return Parent.Namespace; } }

        public PropertyInfoWrapper(TypeInfoWrapper parent, PropertyInfo property)
        {
            Parent = parent;
            Property = property;
        }
    }
}
