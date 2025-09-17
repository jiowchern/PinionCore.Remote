using System;
using System.Linq;
using System.Collections.Generic;
using PinionCore.Network;
using PinionCore.Remote.Gateway.Sessions;
using PinionCore.Extensions;




namespace PinionCore.Remote.Gateway
{
    class SessionInfo
    {
        public GatewaySessionConnector Connector { get; set; }
        public uint Group { get; set; }
    }

    class User
    {
        readonly IStreamable _Stream;
        private readonly HashSet<SessionInfo> _Groups;

        public User(IStreamable streamable)
        {
            _Stream = streamable;
            _Groups = new HashSet<SessionInfo>();
        }

        public void AddGroup(SessionInfo group)
        {
            _Groups.Add(group);
            group.Connector.Join(_Stream);
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
                session.Connector.Leave(_Stream);
                _Groups.Remove(session);
            }
        }

        internal void Stop()
        {
            foreach (var session in _Groups)
            {
                session.Connector.Leave(_Stream);
            }
            _Groups.Clear();
        }
        public bool Equals(IStreamable other)
        {
            return _Stream == other;
        }

        internal void Start()
        {
            foreach (var session in _Groups)
            {
                session.Connector.Join(_Stream);
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
