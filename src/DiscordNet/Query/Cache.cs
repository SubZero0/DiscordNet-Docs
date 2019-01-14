using Discord.Webhook;
using DiscordNet.Query.Wrappers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace DiscordNet.Query
{
    public class Cache
    {
        private ConcurrentDictionary<TypeInfoWrapper, CacheBag> _allTypes;
        private ConcurrentDictionary<TypeInfoWrapper, ConcurrentBag<MethodInfo>> _extensions;
        private int _methodCount, _propertyCount, _extensionMethods, _eventCount;
        private bool _ready;

        public Cache()
        {
            _allTypes = new ConcurrentDictionary<TypeInfoWrapper, CacheBag>();
            _extensions = new ConcurrentDictionary<TypeInfoWrapper, ConcurrentBag<MethodInfo>>();
            _ready = false;
            _methodCount = _propertyCount = _extensionMethods = _eventCount = 0;
        }

        public void Initialize()
        {
            _methodCount = _propertyCount = _extensionMethods = _eventCount = 0;
            _ready = false;
            Populate();
            _ready = true;
            _methodCount = _allTypes.Sum(x => x.Value.Methods.Count);
            _propertyCount = _allTypes.Sum(x => x.Value.Properties.Count);
            _extensionMethods = _extensions.Sum(x => x.Value.Count);
            _eventCount = _allTypes.Sum(x => x.Value.Events.Count);
        }

        public int GetTypeCount()
            => _allTypes.Count;

        public int GetMethodCount()
            => _methodCount;

        public int GetPropertyCount()
            => _propertyCount;

        public int GetEventCount()
            => _eventCount;

        public int GetExtensionTypesCount()
            => _extensions.Count;

        public int GetExtensioMethodsCount()
            => _extensionMethods;

        public CacheBag GetCacheBag(TypeInfoWrapper type)
        {
            if (!_allTypes.ContainsKey(type))
                return null;
            CacheBag cb = new CacheBag(_allTypes[type]);
            foreach (ConcurrentBag<MethodInfo> bag in _extensions.Values)
                foreach (MethodInfo mi in bag)
                    if (CheckTypeAndInterfaces(type, mi.GetParameters().FirstOrDefault()?.ParameterType.GetTypeInfo()))
                        cb.Methods.Add(mi);
            return cb;
        }

        private bool CheckTypeAndInterfaces(TypeInfoWrapper toBeChecked, TypeInfo toSearch)
            => CheckTypeAndInterfaces(toBeChecked.TypeInfo, toSearch);
        private bool CheckTypeAndInterfaces(TypeInfo toBeChecked, TypeInfo toSearch)
        {
            if (toSearch == null || toBeChecked == null)
                return false;
            if (toBeChecked == toSearch)
                return true;
            foreach(Type type in toBeChecked.GetInterfaces())
                if (CheckTypeAndInterfaces(type.GetTypeInfo(), toSearch))
                    return true;
            return false;
        }

        public List<TypeInfoWrapper> SearchTypes(string name, bool exactName = true)
            => _allTypes.Keys.Where(x => (exactName ? x.DisplayName.ToLower() == name.ToLower() : SearchFunction(name, x.DisplayName.ToLower()))).ToList();

        public List<MethodInfoWrapper> SearchMethods(string name, bool exactName = true)
        {
            List<MethodInfoWrapper> result = new List<MethodInfoWrapper>();
            foreach(TypeInfoWrapper type in _allTypes.Keys)
                result.AddRange(GetCacheBag(type).Methods.Where(x => (exactName ? x.Name.ToLower() == name.ToLower() : SearchFunction(name, x.Name.ToLower()))).Select(x => new MethodInfoWrapper(type, x)));
            return result;
        }

        public List<PropertyInfoWrapper> SearchProperties(string name, bool exactName = true)
        {
            List<PropertyInfoWrapper> result = new List<PropertyInfoWrapper>();
            foreach (TypeInfoWrapper type in _allTypes.Keys)
                result.AddRange(GetCacheBag(type).Properties.Where(x => (exactName ? x.Name.ToLower() == name.ToLower() : SearchFunction(name, x.Name.ToLower()))).Select(x => new PropertyInfoWrapper(type, x)));
            return result;
        }

        public List<EventInfoWrapper> SearchEvents(string name, bool exactName = true)
        {
            List<EventInfoWrapper> result = new List<EventInfoWrapper>();
            foreach (TypeInfoWrapper type in _allTypes.Keys)
                result.AddRange(GetCacheBag(type).Events.Where(x => (exactName ? x.Name.ToLower() == name.ToLower() : SearchFunction(name, x.Name.ToLower()))).Select(x => new EventInfoWrapper(type, x)));
            return result;
        }

        private bool SearchFunction(string searchString, string objectName)
        {
            foreach (string s in searchString.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
                if (objectName.IndexOf(s, StringComparison.OrdinalIgnoreCase) == -1)
                    return false;
            return true;
        }

        private void ForceReference()
            => new DiscordWebhookClient(null);

        private void Populate()
        {
            foreach (var a in Assembly.GetExecutingAssembly().GetReferencedAssemblies())
                if (a.Name.StartsWith("Discord") && !a.Name.StartsWith("Discord.Addons"))
                    foreach (Type type in Assembly.Load(a).GetExportedTypes())
                        LoadType(type);
        }

        private void LoadType(Type type)
        {
            if (type.IsGenericParameter)
                type = type.GetGenericTypeDefinition();
            if (!CheckNamespace(type.Namespace))
                return;
            if (_allTypes.Keys.FirstOrDefault(x => x.TypeInfo == type.GetTypeInfo()) == null)
            {
                TypeInfoWrapper tiw = new TypeInfoWrapper(type);
                CacheBag cb = new CacheBag();
                _allTypes[tiw] = cb;
                foreach (MethodInfo mi in type.GetRuntimeMethods())
                {
                    if (CheckNamespace(mi.DeclaringType.Namespace) && (mi.IsPublic || mi.IsFamily) && !mi.IsSpecialName && !cb.Methods.Contains(mi))
                        cb.Methods.Add(mi);
                    if (mi.IsDefined(typeof(ExtensionAttribute), false) && mi.IsStatic && mi.IsPublic)
                    {
                        if (_extensions.Keys.FirstOrDefault(x => x.TypeInfo == type.GetTypeInfo()) == null)
                            _extensions[tiw] = new ConcurrentBag<MethodInfo>(new MethodInfo[] { mi });
                        else
                            _extensions[tiw].Add(mi);
                    }
                }
                var rt = type.GetRuntimeProperties();
                foreach (PropertyInfo pi in type.GetRuntimeProperties())
                    if ((pi.GetMethod.IsFamily || pi.GetMethod.IsPublic) && !cb.Properties.Any(x => x.Name == pi.Name))
                        cb.Properties.Add(pi);
                foreach (EventInfo ei in type.GetRuntimeEvents())
                    cb.Events.Add(ei);
                if(type.GetTypeInfo().IsInterface)
                    foreach (Type t in type.GetInterfaces())
                        LoadInterface(t, tiw);
            }
        }

        private void LoadInterface(Type _interface, TypeInfoWrapper parent)
        {
            if (CheckNamespace(_interface.Namespace))
                LoadType(_interface);
            foreach (MethodInfo mi in _interface.GetRuntimeMethods())
                if (CheckNamespace(mi.DeclaringType.Namespace) && (mi.IsPublic || mi.IsFamily) && !mi.IsSpecialName && !_allTypes[parent].Methods.Contains(mi))
                    if (!_allTypes[parent].Methods.Contains(mi))
                        _allTypes[parent].Methods.Add(mi);
            foreach (PropertyInfo pi in _interface.GetRuntimeProperties())
                if (!_allTypes[parent].Properties.Contains(pi) && !_allTypes[parent].Properties.Any(x => x.Name == pi.Name))
                    _allTypes[parent].Properties.Add(pi);
            foreach (EventInfo ei in _interface.GetRuntimeEvents())
                if (!_allTypes[parent].Events.Contains(ei))
                    _allTypes[parent].Events.Add(ei);
            foreach (Type type in _interface.GetInterfaces())
                LoadInterface(type, parent);
        }

        public bool IsReady()
            => _ready;

        private bool CheckNamespace(string ns)
            => ns.StartsWith("Discord") && !ns.StartsWith("Discord.Net");
    }
}
