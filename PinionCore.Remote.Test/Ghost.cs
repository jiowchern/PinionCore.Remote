﻿using System;

namespace PinionCore.Remote.Test
{
    internal class Ghost : IGhost
    {
        private readonly long _Id;

        public Ghost()
        {
            _Id = 1;
        }



        long IGhost.GetID()
        {
            return _Id;
        }

        public object GetInstance()
        {
            return this;
        }

        private event CallMethodCallback _CallMethodEvent;

        event CallMethodCallback IGhost.CallMethodEvent
        {
            add { _CallMethodEvent += value; }
            remove { _CallMethodEvent -= value; }
        }

        event EventNotifyCallback IGhost.AddEventEvent
        {
            add
            {
                throw new NotImplementedException();
            }

            remove
            {
                throw new NotImplementedException();
            }
        }

        event EventNotifyCallback IGhost.RemoveEventEvent
        {
            add
            {
                throw new NotImplementedException();
            }

            remove
            {
                throw new NotImplementedException();
            }
        }



        bool IGhost.IsReturnType()
        {
            throw new NotImplementedException();
        }

        object IGhost.GetInstance()
        {
            throw new NotImplementedException();
        }


    }
}
