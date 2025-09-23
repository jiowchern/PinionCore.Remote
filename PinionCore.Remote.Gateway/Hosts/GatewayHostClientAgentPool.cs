using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using PinionCore.Remote.Gateway.Protocols;
using PinionCore.Remote.Ghost;

namespace PinionCore.Remote.Gateway.Hosts
{
    class GatewayHostClientAgentPool : IDisposable
    {
        class User
        {
            public IAgent Agent;
            public IClientConnection Session;
            public Servers.GatewayServerSessionAdapter Stream;
            public List<Action> DetachHandlers;
            public Dictionary<Type, HashSet<long>> BridgedGhosts;
            public CancellationTokenSource PumpCancellation;
            public Task PumpTask;
        }

        readonly List<User> _users;
        readonly IProtocol _gameProtocol;
        readonly Dictionary<Type, IReadOnlyList<Type>> _interfaceHierarchy;
        readonly MethodInfo _queryNotifierMethod;
        System.Action _disable;
        public readonly Notifier<IAgent> Agents;
        public readonly IAgent Agent;

        public GatewayHostClientAgentPool(IProtocol gameProtocol)
        {
            if (gameProtocol == null)
            {
                throw new ArgumentNullException(nameof(gameProtocol));
            }

            _users = new List<User>();
            _gameProtocol = gameProtocol;
            Agent = PinionCore.Remote.Gateway.Provider.CreateAgent();
            Agents = new Notifier<IAgent>();

            var interfaceProvider = _gameProtocol.GetInterfaceProvider();
            var protocolInterfaces = new HashSet<Type>(interfaceProvider.Types ?? Array.Empty<Type>());
            _interfaceHierarchy = protocolInterfaces.ToDictionary(
                type => type,
                type => (IReadOnlyList<Type>)type
                    .GetInterfaces()
                    .Where(protocolInterfaces.Contains)
                    .Distinct()
                    .ToArray());

            _queryNotifierMethod = typeof(INotifierQueryable).GetMethod(nameof(INotifierQueryable.QueryNotifier))
                ?? throw new InvalidOperationException("Failed to locate QueryNotifier method.");

            Agent.QueryNotifier<IConnectionManager>().Supply += _Create;
            Agent.QueryNotifier<IConnectionManager>().Unsupply += _Destroy;

            _disable = () =>
            {
                Agent.QueryNotifier<IConnectionManager>().Supply -= _Create;
                Agent.QueryNotifier<IConnectionManager>().Unsupply -= _Destroy;
            };
        }

        private void _Destroy(IConnectionManager owner)
        {
            owner.Connections.Base.Supply -= _Create;
            owner.Connections.Base.Unsupply -= _Destroy;
        }

        private void _Create(IConnectionManager owner)
        {
            owner.Connections.Base.Supply += _Create;
            owner.Connections.Base.Unsupply += _Destroy;
        }

        private void _Destroy(IClientConnection session)
        {
            for (var i = _users.Count - 1; i >= 0; i--)
            {
                var user = _users[i];
                if (!ReferenceEquals(user.Session, session))
                {
                    continue;
                }

                _RemoveUser(user);
                _users.RemoveAt(i);
            }
        }

        private void _Create(IClientConnection session)
        {
            var agent = PinionCore.Remote.Standalone.Provider.CreateAgent(_gameProtocol);
            var stream = new Servers.GatewayServerSessionAdapter(session);
            agent.Enable(stream);

            var user = new User
            {
                Agent = agent,
                Session = session,
                Stream = stream,
                DetachHandlers = new List<Action>(),
                BridgedGhosts = new Dictionary<Type, HashSet<long>>()
            };

            _SetupInterfaceBridging(user);
            _StartPump(user);

            _users.Add(user);
            Agents.Collection.Add(user.Agent);
        }

        public void Dispose()
        {
            for (var i = _users.Count - 1; i >= 0; i--)
            {
                var user = _users[i];
                _RemoveUser(user);
            }

            _users.Clear();
            _disable?.Invoke();
        }

        private void _SetupInterfaceBridging(User user)
        {
            foreach (var kvp in _interfaceHierarchy)
            {
                var derivedType = kvp.Key;
                var baseTypes = kvp.Value;
                if (baseTypes.Count == 0)
                {
                    continue;
                }

                var derivedNotifier = _InvokeQueryNotifier(user.Agent, derivedType);
                if (derivedNotifier == null)
                {
                    continue;
                }

                var notifierType = typeof(INotifier<>).MakeGenericType(derivedType);
                var supplyEvent = notifierType.GetEvent("Supply");
                var unsupplyEvent = notifierType.GetEvent("Unsupply");

                if (supplyEvent == null || unsupplyEvent == null)
                {
                    continue;
                }

                var supplyHandler = _CreateSupplyHandler(user, baseTypes, derivedType);
                var unsupplyHandler = _CreateUnsupplyHandler(user, baseTypes, derivedType);

                supplyEvent.AddEventHandler(derivedNotifier, supplyHandler);
                unsupplyEvent.AddEventHandler(derivedNotifier, unsupplyHandler);

                user.DetachHandlers.Add(() =>
                {
                    supplyEvent.RemoveEventHandler(derivedNotifier, supplyHandler);
                    unsupplyEvent.RemoveEventHandler(derivedNotifier, unsupplyHandler);
                });
            }
        }

        private Delegate _CreateSupplyHandler(User user, IReadOnlyList<Type> baseTypes, Type derivedType)
        {
            var parameter = Expression.Parameter(derivedType, "value");
            var call = Expression.Call(
                Expression.Constant(this),
                typeof(GatewayHostClientAgentPool).GetMethod(nameof(_HandleDerivedSupply), BindingFlags.NonPublic | BindingFlags.Instance)!,
                Expression.Constant(user),
                Expression.Constant(baseTypes),
                Expression.Convert(parameter, typeof(object)));
            var handlerType = typeof(Action<>).MakeGenericType(derivedType);
            return Expression.Lambda(handlerType, call, parameter).Compile();
        }

        private Delegate _CreateUnsupplyHandler(User user, IReadOnlyList<Type> baseTypes, Type derivedType)
        {
            var parameter = Expression.Parameter(derivedType, "value");
            var call = Expression.Call(
                Expression.Constant(this),
                typeof(GatewayHostClientAgentPool).GetMethod(nameof(_HandleDerivedUnsupply), BindingFlags.NonPublic | BindingFlags.Instance)!,
                Expression.Constant(user),
                Expression.Constant(baseTypes),
                Expression.Convert(parameter, typeof(object)));
            var handlerType = typeof(Action<>).MakeGenericType(derivedType);
            return Expression.Lambda(handlerType, call, parameter).Compile();
        }

        private void _HandleDerivedSupply(User user, IReadOnlyList<Type> baseTypes, object instance)
        {
            if (!(instance is IGhost ghost))
            {
                return;
            }

            foreach (var baseType in baseTypes)
            {
                if (!_TryTrackGhost(user, baseType, ghost.GetID()))
                {
                    continue;
                }

                var baseNotifier = _InvokeQueryNotifier(user.Agent, baseType);
                if (baseNotifier is IProvider provider)
                {
                    provider.Add(ghost);
                    provider.Ready(ghost.GetID());
                }
            }
        }

        private void _HandleDerivedUnsupply(User user, IReadOnlyList<Type> baseTypes, object instance)
        {
            if (!(instance is IGhost ghost))
            {
                return;
            }

            foreach (var baseType in baseTypes)
            {
                if (!_TryUntrackGhost(user, baseType, ghost.GetID()))
                {
                    continue;
                }

                var baseNotifier = _InvokeQueryNotifier(user.Agent, baseType);
                if (baseNotifier is IProvider provider)
                {
                    provider.Remove(ghost.GetID());
                }
            }
        }

        private object _InvokeQueryNotifier(IAgent agent, Type interfaceType)
        {
            return _queryNotifierMethod.MakeGenericMethod(interfaceType).Invoke(agent, null);
        }

        private bool _TryTrackGhost(User user, Type baseType, long id)
        {
            if (!user.BridgedGhosts.TryGetValue(baseType, out var ids))
            {
                ids = new HashSet<long>();
                user.BridgedGhosts[baseType] = ids;
            }

            return ids.Add(id);
        }

        private bool _TryUntrackGhost(User user, Type baseType, long id)
        {
            if (!user.BridgedGhosts.TryGetValue(baseType, out var ids))
            {
                return false;
            }

            return ids.Remove(id);
        }

        private void _RemoveBridgedGhosts(User user)
        {
            foreach (var kvp in user.BridgedGhosts)
            {
                var baseNotifier = _InvokeQueryNotifier(user.Agent, kvp.Key);
                if (!(baseNotifier is IProvider provider))
                {
                    continue;
                }

                foreach (var id in kvp.Value.ToList())
                {
                    provider.Remove(id);
                }
            }

            user.BridgedGhosts.Clear();
        }

        private void _DetachHandlers(User user)
        {
            if (user.DetachHandlers == null)
            {
                return;
            }

            foreach (var detach in user.DetachHandlers)
            {
                detach();
            }

            user.DetachHandlers.Clear();
        }

        private void _StartPump(User user)
        {
            var cts = new CancellationTokenSource();
            user.PumpCancellation = cts;
            user.PumpTask = Task.Run(async () =>
            {
                var token = cts.Token;
                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        user.Agent.HandlePackets();
                        user.Agent.HandleMessage();
                        await Task.Delay(1, token).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException)
                {
                }
                catch (ObjectDisposedException)
                {
                }
            }, cts.Token);
        }

        private void _StopPump(User user)
        {
            if (user.PumpCancellation == null)
            {
                return;
            }

            try
            {
                user.PumpCancellation.Cancel();
                user.PumpTask?.Wait();
            }
            catch (AggregateException ex) when (ex.InnerException is OperationCanceledException)
            {
            }
            finally
            {
                user.PumpCancellation.Dispose();
                user.PumpCancellation = null;
                user.PumpTask = null;
            }
        }

        private void _RemoveUser(User user)
        {
            Agents.Collection.Remove(user.Agent);
            _StopPump(user);
            _RemoveBridgedGhosts(user);
            _DetachHandlers(user);
            user.Agent.Disable();
            user.Stream?.Dispose();
        }
    }
}



