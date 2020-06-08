using System.Reflection;

namespace DiscordNet.Query
{
    public class EventInfoWrapper : BaseInfoWrapper
    {
        public EventInfo Event { get; private set; }
        public TypeInfoWrapper Parent { get; private set; }
        public string Namespace { get { return Parent.Namespace; } }

        public EventInfoWrapper(TypeInfoWrapper parent, EventInfo e)
        {
            Parent = parent;
            Event = e;
        }
    }
}
