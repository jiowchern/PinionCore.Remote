using System;
using System.Collections.Generic;
using PinionCore.Extensions;

namespace PinionCore.Remote
{
    namespace ProviderHelper
    {
        public class GhostsOwner
        {
            private readonly Dictionary<Type, IProvider> _Providers;
            private readonly IProtocol _Protocol;

            public GhostsOwner(IProtocol protocol)
            {
                _Protocol = protocol;
                PinionCore.Utility.Log.Instance.WriteInfo("GhostsOwner Protocol: " + protocol.VersionCode.ToMd5String());
                _Providers = new Dictionary<Type, IProvider>();
            }

            public IProvider QueryProvider(Type type)
            {
                lock (_Providers)
                {
                    if (!_Providers.TryGetValue(type, out IProvider provider))
                    {
                        provider = BuildProvider(type);
                        _Providers.Add(type, provider);
                    }
                    return provider;
                }
            }

            private IProvider BuildProvider(Type type)
            {
                MemberMap map = _Protocol.GetMemberMap();
                return map.CreateProvider(type);
            }

            public INotifier<T> QueryProvider<T>()
            {
                return QueryProvider(typeof(T)) as INotifier<T>;
            }

            public void ClearProviders()
            {
                lock (_Providers)
                {
                    foreach (IProvider provider in _Providers.Values)
                    {
                        provider.ClearGhosts();
                    }
                    //_Providers.Clear();
                }
            }

            public void RemoveGhost(long id)
            {
                lock (_Providers)
                {
                    foreach (IProvider provider in _Providers.Values)
                    {
                        provider.Remove(id);
                    }
                }
            }
        }

    }
}
