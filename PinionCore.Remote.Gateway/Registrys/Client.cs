using System;
using System.Collections.Generic;
using PinionCore.Network;
using PinionCore.Remote.Gateway.Protocols;
using PinionCore.Remote.Ghost;
using PinionCore.Remote;

namespace PinionCore.Remote.Gateway.Registrys
{
    public class Client : IDisposable
    {
        private readonly uint _Group;
        private readonly byte[] _Version;
        private readonly INotifierQueryable _Queryer;
        private readonly INotifier<IRegisterable> _RegisterNotifier;
        private readonly object _SessionLock = new object();
        private readonly List<RegisterableSession> _Sessions;
        private readonly Depot<IStreamable> _Streams;
        private readonly Notifier<IStreamable> _Notifier;
        private bool _Disposed;

        public readonly PinionCore.Remote.Soul.IListenable Listener;
        public readonly IAgent Agent;

        public Client(uint group, byte[] version)
        {
            _Group = group;
            _Version = version != null ? (byte[])version.Clone() : Array.Empty<byte>();
            _Streams = new Depot<IStreamable>();
            _Notifier = new Notifier<IStreamable>(_Streams);
            Listener = new PinionCore.Remote.Gateway.Misc.NotifierListener(_Notifier);
            Agent = Provider.CreateAgent();
            _Queryer = Agent;
            _Sessions = new List<RegisterableSession>();
            _RegisterNotifier = _Queryer.QueryNotifier<IRegisterable>();
            _RegisterNotifier.Supply += OnRegisterableSupplied;
            _RegisterNotifier.Unsupply += OnRegisterableUnsupplied;
        }

        private void OnRegisterableSupplied(IRegisterable registerable)
        {
            if (_Disposed)
            {
                return;
            }

            var session = new RegisterableSession(this, registerable);

            lock (_SessionLock)
            {
                _Sessions.Add(session);
            }

            session.Start();
        }

        private void OnRegisterableUnsupplied(IRegisterable registerable)
        {
            RegisterableSession session = null;

            lock (_SessionLock)
            {
                for (int i = 0; i < _Sessions.Count; i++)
                {
                    if (_Sessions[i].Matches(registerable))
                    {
                        session = _Sessions[i];
                        _Sessions.RemoveAt(i);
                        break;
                    }
                }
            }

            session?.Dispose();
        }

        public void Dispose()
        {
            if (_Disposed)
            {
                return;
            }

            _Disposed = true;

            _Streams.Items.Clear();
            _Notifier.Dispose();
            _RegisterNotifier.Supply -= OnRegisterableSupplied;
            _RegisterNotifier.Unsupply -= OnRegisterableUnsupplied;

            lock (_SessionLock)
            {
                foreach (RegisterableSession session in _Sessions)
                {
                    session.Dispose();
                }

                _Sessions.Clear();
            }
        }

        internal void AddStream(IStreamable stream)
        {
            if (_Disposed)
            {
                return;
            }

            _Streams.Items.Add(stream);
            Console.WriteLine("Client stream supplied");
        }

        internal void RemoveStream(IStreamable stream)
        {
            _Streams.Items.Remove(stream);
        }

        private sealed class RegisterableSession : IDisposable
        {
            private readonly Client _Owner;
            private readonly IRegisterable _Registerable;
            private bool _StreamsSubscribed;
            private bool _Disposed;
            private IStreamProviable _CurrentProvider;
            private Action<IStreamable> _StreamSupplyHandler;
            private Action<IStreamable> _StreamUnsupplyHandler;

            public RegisterableSession(Client owner, IRegisterable registerable)
            {
                _Owner = owner;
                _Registerable = registerable;
            }

            public bool Matches(IRegisterable registerable)
            {
                return ReferenceEquals(_Registerable, registerable);
            }

            public void Start()
            {
                _Registerable.LoginNotifier.Base.Supply += OnLoginSupplied;
                _Registerable.LoginNotifier.Base.Unsupply += OnLoginUnsupplied;
            }

            private void OnLoginSupplied(ILoginable loginable)
            {
                _Registerable.LoginNotifier.Base.Supply -= OnLoginSupplied;
                _Registerable.LoginNotifier.Base.Unsupply -= OnLoginUnsupplied;

                CompleteLoginAsync(loginable);
            }

            private void OnLoginUnsupplied(ILoginable loginable)
            {
                _Registerable.LoginNotifier.Base.Supply -= OnLoginSupplied;
                _Registerable.LoginNotifier.Base.Unsupply -= OnLoginUnsupplied;
            }

            private async void CompleteLoginAsync(ILoginable loginable)
            {
                try
                {
                    await loginable.Login(_Owner._Group, _Owner._Version);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Client login failed: {ex}");
                    return;
                }

                if (_Disposed)
                {
                    return;
                }

                SubscribeStreams();
            }

            private void SubscribeStreams()
            {
                if (_StreamsSubscribed || _Disposed)
                {
                    return;
                }

                _StreamsSubscribed = true;
                _Registerable.StreamsNotifier.Base.Supply += OnStreamSupplied;
                _Registerable.StreamsNotifier.Base.Unsupply += OnStreamUnsupplied;
            }

            private void OnStreamSupplied(IStreamProviable proviable)
            {
                if (_Disposed)
                {
                    return;
                }

                if (_CurrentProvider != null && !ReferenceEquals(_CurrentProvider, proviable))
                {
                    throw new Exception("Already registered stream provider.");
                }

                if (_CurrentProvider == null)
                {
                    AttachProvider(proviable);
                }
            }

            private void OnStreamUnsupplied(IStreamProviable proviable)
            {
                if (!ReferenceEquals(_CurrentProvider, proviable))
                {
                    return;
                }

                DetachProvider();
            }

            private void AttachProvider(IStreamProviable proviable)
            {
                _CurrentProvider = proviable;

                _StreamSupplyHandler = stream =>
                {
                    if (_Disposed)
                    {
                        return;
                    }

                    _Owner.AddStream(stream);
                };

                _StreamUnsupplyHandler = stream =>
                {
                    _Owner.RemoveStream(stream);
                };

                _CurrentProvider.Streams.Base.Supply += _StreamSupplyHandler;
                _CurrentProvider.Streams.Base.Unsupply += _StreamUnsupplyHandler;
                Console.WriteLine("Client received stream provider");
            }

            private void DetachProvider()
            {
                if (_CurrentProvider == null)
                {
                    return;
                }

                if (_StreamSupplyHandler != null)
                {
                    _CurrentProvider.Streams.Base.Supply -= _StreamSupplyHandler;
                }

                if (_StreamUnsupplyHandler != null)
                {
                    _CurrentProvider.Streams.Base.Unsupply -= _StreamUnsupplyHandler;
                }

                _StreamSupplyHandler = null;
                _StreamUnsupplyHandler = null;
                _CurrentProvider = null;
            }

            public void Dispose()
            {
                if (_Disposed)
                {
                    return;
                }

                _Disposed = true;
                _Registerable.LoginNotifier.Base.Supply -= OnLoginSupplied;
                _Registerable.LoginNotifier.Base.Unsupply -= OnLoginUnsupplied;

                if (_StreamsSubscribed)
                {
                    _Registerable.StreamsNotifier.Base.Supply -= OnStreamSupplied;
                    _Registerable.StreamsNotifier.Base.Unsupply -= OnStreamUnsupplied;
                }

                DetachProvider();
            }
        }
    }
}
