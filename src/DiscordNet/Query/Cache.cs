using DiscordNet.Query.Wrappers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace DiscordNet.Query
{
    public class CacheBag
    {
        public ConcurrentBag<MethodInfo> Methods;
        public ConcurrentBag<PropertyInfo> Properties;
        public CacheBag()
        {
            Methods = new ConcurrentBag<MethodInfo>();
            Properties = new ConcurrentBag<PropertyInfo>();
        }
    }
    public class Cache
    {
        private ConcurrentDictionary<TypeInfo, CacheBag> cache;
        private int methodCount, propertyCount;
        private bool ready;
        public Cache()
        {
            cache = new ConcurrentDictionary<TypeInfo, CacheBag>();
            ready = false;
            methodCount = propertyCount = 0;
        }

        public void Initialize()
        {
            methodCount = propertyCount = 0;
            ready = false;
            Populate();
            ready = true;
            methodCount = cache.Sum(x => x.Value.Methods.Count);
            propertyCount = cache.Sum(x => x.Value.Properties.Count);
        }

        public int GetTypeCount()
        {
            return cache.Count;
        }

        public int GetMethodCount()
        {
            return methodCount;
        }

        public int GetPropertyCount()
        {
            return propertyCount;
        }

        public CacheBag GetCacheBag(TypeInfo type)
        {
            if (!cache.ContainsKey(type))
                return null;
            return cache[type];
        }

        public List<TypeInfo> SearchTypes(string name, bool exactName = true)
        {
            return cache.Keys.Where(x => (exactName ? x.Name.ToLower() == name.ToLower() : SearchFunction(name, x.Name))).ToList();
        }

        public List<MethodInfoWrapper> SearchMethods(string name, bool exactName = true)
        {
            List<MethodInfoWrapper> result = new List<MethodInfoWrapper>();
            foreach(TypeInfo type in cache.Keys)
                result.AddRange(cache[type].Methods.Where(x => (exactName ? x.Name.ToLower() == name.ToLower() : SearchFunction(name, x.Name))).Select(x => new MethodInfoWrapper(type, x)));
            return result;
        }

        public List<PropertyInfoWrapper> SearchProperties(string name, bool exactName = true)
        {
            List<PropertyInfoWrapper> result = new List<PropertyInfoWrapper>();
            foreach (TypeInfo type in cache.Keys)
                result.AddRange(cache[type].Properties.Where(x => (exactName ? x.Name.ToLower() == name.ToLower() : SearchFunction(name, x.Name))).Select(x => new PropertyInfoWrapper(type, x)));
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
            if(!cache.ContainsKey(type.GetTypeInfo()))
            {
                CacheBag cb = new CacheBag();
                cache[type.GetTypeInfo()] = cb;
                foreach (MethodInfo mi in type.GetMethods())
                    if (mi.IsPublic && !mi.IsSpecialName)
                        cb.Methods.Add(mi);
                foreach (PropertyInfo pi in type.GetProperties())
                    cb.Properties.Add(pi);
                foreach (Type t in type.GetInterfaces())
                    LoadInterface(t, type);
            }
        }

        private void LoadInterface(Type _interface, Type parent)
        {
            LoadType(_interface);
            foreach (MethodInfo mi in _interface.GetMethods())
                if(mi.IsPublic && !mi.IsSpecialName)
                    if(!cache[parent.GetTypeInfo()].Methods.Contains(mi))
                        cache[parent.GetTypeInfo()].Methods.Add(mi);
            foreach (PropertyInfo pi in _interface.GetProperties())
                if (!cache[parent.GetTypeInfo()].Properties.Contains(pi))
                    cache[parent.GetTypeInfo()].Properties.Add(pi);
            foreach (Type type in _interface.GetInterfaces())
                LoadInterface(type, parent);
        }

        public bool IsReady()
        {
            return ready;
        }
    }
}
