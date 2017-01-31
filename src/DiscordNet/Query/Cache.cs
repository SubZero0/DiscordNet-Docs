using DiscordNet.Query.Wrappers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

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
    public class Cache
    {
        private ConcurrentDictionary<TypeInfo, CacheBag> allTypes;
        private ConcurrentDictionary<TypeInfo, ConcurrentBag<MethodInfo>> extensions;
        private int methodCount, propertyCount, extensionMethods, eventCount;
        private bool ready;
        public Cache()
        {
            allTypes = new ConcurrentDictionary<TypeInfo, CacheBag>();
            extensions = new ConcurrentDictionary<TypeInfo, ConcurrentBag<MethodInfo>>();
            ready = false;
            methodCount = propertyCount = extensionMethods = eventCount = 0;
        }

        public void Initialize()
        {
            methodCount = propertyCount = 0;
            ready = false;
            Populate();
            ready = true;
            methodCount = allTypes.Sum(x => x.Value.Methods.Count);
            propertyCount = allTypes.Sum(x => x.Value.Properties.Count);
            extensionMethods = extensions.Sum(x => x.Value.Count);
            eventCount = allTypes.Sum(x => x.Value.Events.Count);
        }

        public int GetTypeCount()
        {
            return allTypes.Count;
        }

        public int GetMethodCount()
        {
            return methodCount;
        }

        public int GetPropertyCount()
        {
            return propertyCount;
        }

        public int GetEventCount()
        {
            return eventCount;
        }

        public int GetExtensionTypesCount()
        {
            return extensions.Count;
        }

        public int GetExtensioMethodsCount()
        {
            return extensionMethods;
        }

        public CacheBag GetCacheBag(TypeInfo type)
        {
            if (!allTypes.ContainsKey(type))
                return null;
            CacheBag cb = new CacheBag(allTypes[type]);
            foreach (ConcurrentBag<MethodInfo> bag in extensions.Values)
                foreach (MethodInfo mi in bag)
                    if (CheckTypeAndInterfaces(type, mi.GetParameters().First().ParameterType.GetTypeInfo()))
                        cb.Methods.Add(mi);
            return cb;
        }

        private bool CheckTypeAndInterfaces(TypeInfo toBeChecked, TypeInfo toSearch)
        {
            if (toBeChecked == toSearch)
                return true;
            foreach(Type type in toBeChecked.GetInterfaces())
                if (CheckTypeAndInterfaces(type.GetTypeInfo(), toSearch))
                    return true;
            return false;
        }

        public List<TypeInfo> SearchTypes(string name, bool exactName = true)
        {
            return allTypes.Keys.Where(x => (exactName ? x.Name.ToLower() == name.ToLower() : SearchFunction(name, x.Name))).ToList();
        }

        public List<MethodInfoWrapper> SearchMethods(string name, bool exactName = true)
        {
            List<MethodInfoWrapper> result = new List<MethodInfoWrapper>();
            foreach(TypeInfo type in allTypes.Keys)
                result.AddRange(GetCacheBag(type).Methods.Where(x => (exactName ? x.Name.ToLower() == name.ToLower() : SearchFunction(name, x.Name))).Select(x => new MethodInfoWrapper(type, x)));
            return result;
        }

        public List<PropertyInfoWrapper> SearchProperties(string name, bool exactName = true)
        {
            List<PropertyInfoWrapper> result = new List<PropertyInfoWrapper>();
            foreach (TypeInfo type in allTypes.Keys)
                result.AddRange(GetCacheBag(type).Properties.Where(x => (exactName ? x.Name.ToLower() == name.ToLower() : SearchFunction(name, x.Name))).Select(x => new PropertyInfoWrapper(type, x)));
            return result;
        }

        public List<EventInfo> SearchEvents(string name, bool exactName = true)
        {
            List<EventInfo> result = new List<EventInfo>();
            foreach (TypeInfo type in allTypes.Keys)
                result.AddRange(GetCacheBag(type).Events.Where(x => (exactName ? x.Name.ToLower() == name.ToLower() : SearchFunction(name, x.Name))));
            return result;
        }

        private bool SearchFunction(string searchString, string objectName)
        {
            foreach (string s in searchString.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
                if (objectName.IndexOf(s, StringComparison.OrdinalIgnoreCase) == -1)
                    return false;
            return true;
        }

        private void Populate()
        {
            List<TypeInfo> list = new List<TypeInfo>();
            foreach (var a in Assembly.GetEntryAssembly().GetReferencedAssemblies())
                if (a.Name.StartsWith("Discord"))
                    foreach (Type type in Assembly.Load(a).GetExportedTypes())
                        LoadType(type);
        }

        private void LoadType(Type type)
        {
            if(!allTypes.ContainsKey(type.GetTypeInfo()))
            {
                CacheBag cb = new CacheBag();
                allTypes[type.GetTypeInfo()] = cb;
                foreach (MethodInfo mi in type.GetMethods())
                {
                    if (mi.IsPublic && !mi.IsSpecialName)
                        cb.Methods.Add(mi);
                    if (mi.IsDefined(typeof(ExtensionAttribute), false) && mi.IsStatic && mi.IsPublic)
                    {
                        if (!extensions.ContainsKey(type.GetTypeInfo()))
                            extensions[type.GetTypeInfo()] = new ConcurrentBag<MethodInfo>(new MethodInfo[] { mi });
                        else
                            extensions[type.GetTypeInfo()].Add(mi);
                    }
                }
                foreach (PropertyInfo pi in type.GetProperties())
                    cb.Properties.Add(pi);
                foreach (EventInfo ei in type.GetEvents())
                    cb.Events.Add(ei);
                foreach (Type t in type.GetInterfaces())
                    LoadInterface(t, type);
            }
        }

        private void LoadInterface(Type _interface, Type parent)
        {
            LoadType(_interface);
            foreach (MethodInfo mi in _interface.GetMethods())
                if(mi.IsPublic && !mi.IsSpecialName)
                    if(!allTypes[parent.GetTypeInfo()].Methods.Contains(mi))
                        allTypes[parent.GetTypeInfo()].Methods.Add(mi);
            foreach (PropertyInfo pi in _interface.GetProperties())
                if (!allTypes[parent.GetTypeInfo()].Properties.Contains(pi))
                    allTypes[parent.GetTypeInfo()].Properties.Add(pi);
            foreach (EventInfo ei in _interface.GetEvents())
                if (!allTypes[parent.GetTypeInfo()].Events.Contains(ei))
                    allTypes[parent.GetTypeInfo()].Events.Add(ei);
            foreach (Type type in _interface.GetInterfaces())
                LoadInterface(type, parent);
        }

        public bool IsReady()
        {
            return ready;
        }
    }
}
