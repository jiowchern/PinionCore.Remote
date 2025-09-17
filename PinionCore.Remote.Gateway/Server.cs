using System;
using System.Linq;
using System.Collections.Generic;
using PinionCore.Network;
using PinionCore.Remote.Gateway.Sessions;
using PinionCore.Extensions;




namespace PinionCore.Remote.Gateway
{

    struct Package {
        public uint Group;
        public ArraySegment<byte> Payload;
    }
    class SessionInfo
    {
        public GatewaySessionConnector Connector { get; set; }
        public uint Group { get; set; }
    }

    class User
    {
        readonly PinionCore.Network.Stream _Server;
        readonly IStreamable _Client;
        private readonly HashSet<SessionInfo> _Groups;
        private bool _started;

        public User(IStreamable streamable)
        {
            _Server = new Stream();
            _Client = streamable;
            _Groups = new HashSet<SessionInfo>();
            _started = false;

            /*
                todo: 連接 _Client 與 _Server 之間的訊息轉送

                把 _Server 收到的消息用 struct PinionCore.Remote.Gateway.Package 包裝發送給 _Client
                把 _Client 收到的消息是 struct PinionCore.Remote.Gateway.Package 解包後發送給 _Server
                慮用 DataflowActor 來做這個轉送
            */
        }

        public void AddGroup(SessionInfo group)
        {
            if (_Groups.Add(group) && _started)
            {
                _Join(group);

            }
        }

        private void _Join(SessionInfo group)
        {                        
            group.Connector.Join(_Server);
        }

        public bool HasGroup(uint group)
        {
            return _Groups.Any(g => g.Group == group);
        }

        internal void RemoveGroup(uint group)
        {
            var session = _Groups.FirstOrDefault(g => g.Group == group);
            if (session != null)
            {
                if (_started)
                {
                    session.Connector.Leave(_Server);
                }
                _Groups.Remove(session);
            }
        }

        internal void Stop()
        {
            if (_started)
            {
                foreach (var session in _Groups)
                {
                    session.Connector.Leave(_Client);
                }
            }
            _Groups.Clear();
            _started = false;
        }
        public bool Equals(IStreamable other)
        {
            return _Client == other;
        }

        internal void Start()
        {
            if (_started)
            {
                return;
            }

            _started = true;

            foreach (var session in _Groups)
            {
                _Join(session);                
            }
        }
    }


    class Server : System.IDisposable
    {
        readonly System.Collections.Generic.List<SessionInfo> _Sessions;
        readonly System.Collections.Generic.List<User> _Users;

        public Server() {
            _Sessions = new List<SessionInfo>();
            _Users = new List<User>();
        }
        
        internal void Register(uint group, GatewaySessionConnector connector)
        {
            var info = new SessionInfo { Group = group, Connector = connector };
            _Sessions.Add(info);

            _NotifyUsersAdd(info);
        }
        internal void Unregister(GatewaySessionConnector connector)
        {
            var session = _Sessions.FirstOrDefault(s => s.Connector == connector);
            if (session != null)
            {
                _Sessions.Remove(session);
                _NotifyUsersRemove(session);
            }
        }

        private void _NotifyUsersRemove(SessionInfo session)
        {
            foreach (var user in _Users)
            {
                if (user.HasGroup(session.Group))
                {
                    user.RemoveGroup(session.Group);
                }
            }
        }

        private void _NotifyUsersAdd(SessionInfo info)
        {
            foreach (var user in _Users)
            {
                if (!user.HasGroup(info.Group))
                {
                    user.AddGroup(info);
                }
            }
        }
        public void Join(IStreamable streamable)
        {
            var user = new User(streamable);
            
            foreach (var session in _Sessions.Shuffle())
            {
                if (!user.HasGroup(session.Group))
                {
                    user.AddGroup(session);
                }
            }
            user.Start();
            _Users.Add(user);
        }

        public void Leave(IStreamable streamable)
        {
            var user = _Users.FirstOrDefault(u => u.Equals(streamable));
            if (user != null)
            {
                user.Stop();
                _Users.Remove(user);
            }
        }

        void IDisposable.Dispose()
        {
            foreach (var user in _Users)
            {
                user.Stop();
            }
        }
    }
}
