﻿using System;
using System.Collections.Generic;

namespace Regulus.Remote.Client
{
    public class User
    {
        public readonly IAgent Agent;
        public readonly string Id;
        

        readonly List<Tuple<Type, object>> _Ghosts;
        public IEnumerable<Tuple<Type, object>> Ghosts => _Ghosts;
        public User(IAgent agent , AgentEventRectifier rectifier)
        {
            Id = System.Guid.NewGuid().ToString();
            _Ghosts = new List<Tuple<Type, object>>();
            this.Agent = agent;
            
            rectifier.SupplyEvent += _Add;
            rectifier.UnsupplyEvent += _Remove;
            Dispose = () => {
                rectifier.SupplyEvent -= _Add;
                rectifier.UnsupplyEvent -= _Remove;
                (rectifier as IDisposable).Dispose();
            };
        }

        private void _Remove(Type arg1, object arg2)
        {
            _Ghosts.RemoveAll((g) => g.Item2 == arg2);
        }

        private void _Add(Type arg1, object arg2)
        {
            _Ghosts.Add(new Tuple<Type, object>(arg1 , arg2));
        }

        public readonly System.Action Dispose;
        

        
    }
}