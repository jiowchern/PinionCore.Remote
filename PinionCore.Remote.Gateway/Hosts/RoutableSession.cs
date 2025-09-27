using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using PinionCore.Remote.Gateway.Protocols;

namespace PinionCore.Remote.Gateway.Hosts
{
    class RoutableSession
    {
        public enum GroupState
        {
            Requiering,
            Done
                
        }

        readonly IRoutableSession _Session;

        public RoutableSession(IRoutableSession session)
        {
            _Session = session;
            _Groups = new Dictionary<uint, GroupState>();
            Groups = _Groups;
            _Conns = new Dictionary<uint, IConnection>();
        }


        readonly Dictionary<uint, GroupState> _Groups;
        readonly Dictionary<uint, IConnection> _Conns;
        public readonly IReadOnlyDictionary<uint, GroupState> Groups;

        internal void Borrow(uint group, IConnection value)
        {
            if(!_Conns.TryAdd(group, value))
            {
                throw new Exception("");
            }
            _Groups.Remove(group);
            _Groups.Add(group, GroupState.Done);
            _Session.Set(group, value);

        }

        internal IConnection Return(uint group)
        {
            if(_Conns.TryGetValue(group , out var conn))
            {
                _Groups.Remove(group);
                _Conns.Remove(group);
                _Session.Unset(group);

                return conn;
            }

            throw new Exception("");
        }

        internal bool Release(IConnection connection)
        {
            var removeGroups = new List<uint>();
            foreach (var pair in _Conns)
            {
                if(pair.Value == connection)
                {
                    removeGroups.Add(pair.Key);
                }
            }

            foreach(var g in removeGroups)
            {
                _Conns.Remove(g);
                _Groups.Remove(g);
                _Session.Unset(g);
            }
            return removeGroups.Count > 0;
        }

        internal void SetRequiering(uint group)
        {
            _Groups.Add(group, GroupState.Requiering);
        }

        internal void Reset(uint group)
        {
            if(_Groups.ContainsKey(group))
            {
                _Groups.Remove(group);                
            }
            if(_Conns.ContainsKey(group))
            {
                _Conns.Remove(group);
                _Session.Unset(group);
            }
        }
    }
}


