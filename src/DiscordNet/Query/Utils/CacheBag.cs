using System.Collections.Concurrent;
using System.Reflection;

namespace DiscordNet.Query
{
    public class CacheBag
    {
        public ConcurrentBag<MethodInfo> Methods;
        public ConcurrentBag<PropertyInfo> Properties;
        public ConcurrentBag<EventInfo> Events;
        public CacheBag()
        {
            Methods = new ConcurrentBag<MethodInfo>();
            Properties = new ConcurrentBag<PropertyInfo>();
            Events = new ConcurrentBag<EventInfo>();
        }
        public CacheBag(CacheBag cb)
        {
            Methods = new ConcurrentBag<MethodInfo>(cb.Methods);
            Properties = new ConcurrentBag<PropertyInfo>(cb.Properties);
            Events = new ConcurrentBag<EventInfo>(cb.Events);
        }
    }
}
