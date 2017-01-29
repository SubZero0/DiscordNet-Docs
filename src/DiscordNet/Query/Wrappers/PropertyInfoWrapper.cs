using System.Reflection;

namespace DiscordNet.Query.Wrappers
{
    public class PropertyInfoWrapper
    {
        public PropertyInfo Property { get; private set; }
        public TypeInfo Parent { get; private set; }
        public PropertyInfoWrapper(TypeInfo parent, PropertyInfo property)
        {
            Parent = parent;
            Property = property;
        }
    }
}
