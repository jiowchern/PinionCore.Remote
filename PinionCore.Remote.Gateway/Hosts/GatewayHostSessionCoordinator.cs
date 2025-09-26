using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using PinionCore.Remote.Actors;
using PinionCore.Remote.Gateway.Protocols;

namespace PinionCore.Remote.Gateway.Hosts
{
    public interface IActotCommand
    {

    }
    internal class GatewayHostSessionCoordinator : ISessionMembership, IServiceRegistry , IDisposable
    {
        

        private readonly IGameLobbySelectionStrategy _Strategy;
        readonly List<RoutableSession> _RoutableSessions;
        readonly System.Collections.Generic.Dictionary<uint, ClientConnectionDisposer> _Disposers;
        

        struct LobbyCommand : IActotCommand
        {
            public uint Group;
            public IGameLobby Lobby;
            public bool Register;
        }

        struct DisposerRequierCommand : IActotCommand
        {
            public uint Group;
            public IClientConnection ClientConnection;
            public RoutableSession RoutableSession;
        }

        struct SessionCommand : IActotCommand
        {
            public IRoutableSession Session;
            public bool Join;
        }

        readonly DataflowActor<IActotCommand> _DataflowActor;

        public GatewayHostSessionCoordinator(IGameLobbySelectionStrategy strategy)
        {
            _Strategy = strategy;
            _RoutableSessions = new List<RoutableSession>();
            _DataflowActor = new DataflowActor<IActotCommand>(_HandleCommand);
            
            _Disposers = new Dictionary<uint, ClientConnectionDisposer>();
        }

        

        void ISessionMembership.Join(IRoutableSession session)
        {
            _DataflowActor.Post(new SessionCommand { Session = session, Join = true });
            
        }

        void ISessionMembership.Leave(IRoutableSession session)
        {
            _DataflowActor.Post(new SessionCommand { Session = session, Join = false });
            
        }

        void IServiceRegistry.Register(uint group, IGameLobby lobby)
        {
            _DataflowActor.Post(new LobbyCommand { Lobby = lobby, Group = group, Register = true });
            
        }

        void IServiceRegistry.Unregister(uint group, IGameLobby lobby)
        {
            _DataflowActor.Post(new LobbyCommand { Group = group, Lobby = lobby, Register = false });
            
        }

        public void Dispose()
        {            
        }

        private async Task _HandleCommand(IActotCommand command, CancellationToken token)
        {
            switch (command)
            {
                case LobbyCommand dc:
                    {
                        if (dc.Register)
                        {
                            _JoinLobby(dc.Group, dc.Lobby);
                        }
                        else
                        {
                            _LeaveLobby(dc.Group, dc.Lobby);
                        }
                    }
                    break;
                case SessionCommand sc:
                    {
                        if (sc.Join)
                        {
                            _JoinSession(sc.Session);
                        }
                        else
                        {
                            _LeaveSession(sc.Session);
                        }
                    }
                    break;
                case DisposerRequierCommand disposerRequierCommand:
                    {
                        _Borrow(disposerRequierCommand.RoutableSession, disposerRequierCommand.ClientConnection, disposerRequierCommand.Group);
                    }
                    break;
            }
        }

        private void _Borrow(RoutableSession routableSession, IClientConnection clientConnection, uint group)
        {
            if(!_RoutableSessions.Contains(routableSession))
            {
                return;
            }
            routableSession.Borrow(group, clientConnection);
        }

        private void _LeaveSession(IRoutableSession session)
        {
            var rs = _RoutableSessions.FirstOrDefault(s => s == session);
            if (rs != null)
            {
                _RoutableSessions.Remove(rs);
                _RetuenDisposers(rs);
            }
        }

        private void _RetuenDisposers(RoutableSession rs)
        {
            var needReturns = rs.Groups;
            foreach (var group in needReturns)
            {
                
                var session = rs.Return(group.Key);
                if (_Disposers.TryGetValue(group.Key, out var disposer))
                {                    
                    disposer.Return(session);
                }
            }


        }

        private void _JoinSession(IRoutableSession session)
        {
            var rs = new RoutableSession(session);
            _RoutableSessions.Add(rs);
            _RequierDisposers(rs);
        }

        private void _RequierDisposers(RoutableSession rs)
        {
            var needRequires =  _Disposers.Keys.Except(rs.Groups.Select(g=>g.Key));
            foreach(var group in needRequires)
            {
                _PostRequier(rs, group);
            }
        }

        private void _PostRequier(RoutableSession rs, uint group)
        {
            if(rs.Groups.Any( g=>g.Key == group))
            {
                return;
            }
            rs.SetRequiering(group);
            var val = _Disposers[group].Require();
            val.OnValue += (s) =>
            {
                _DataflowActor.Post(new DisposerRequierCommand { RoutableSession = rs, ClientConnection = s, Group = group });
            };
        }

        private void _LeaveLobby(uint group, IGameLobby lobby)
        {
            if (_Disposers.TryGetValue(group, out var disposer))
            {
                disposer.Remove(lobby);
                if (disposer.IsEmpty())
                {                    
                    _Disposers.Remove(group);                    
                    disposer.Dispose();
                    disposer.ClientReleasedEvent -= _ReleaseSession;
                }                
                
            }
            
        }

      
        private void _JoinLobby(uint group, IGameLobby lobby)
        {
            if (_Disposers.TryGetValue(group, out var disposer))
            {
                disposer.Add(lobby);
            }
            else
            {
                disposer = new ClientConnectionDisposer(_Strategy);
                disposer.ClientReleasedEvent += _ReleaseSession;
                _Disposers.Add(group, disposer);
                disposer.Add(lobby);
            }

            _UpdateSessionGroup(group);
        }

        private void _ReleaseSession(IClientConnection connection)
        {
            foreach (var rs in _RoutableSessions)
            {
                rs.Release(connection);
            }
        }

        private void _UpdateSessionGroup(uint group)
        {
            foreach (var rs in _RoutableSessions)
            {
                _PostRequier(rs, group);                
            }
        }

       
    }
}


