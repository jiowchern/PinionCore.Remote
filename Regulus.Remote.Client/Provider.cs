﻿using Regulus.Network;
using Regulus.Remote.Ghost;
using System;

namespace Regulus.Remote.Client
{



    public class Provider
    {        
        public static Ghost.IAgent CreateAgent(IProtocol protocol, IStreamable stream, ISerializable serializable)
        {
            return new Ghost.Agent(stream , protocol, serializable , new Regulus.Remote.InternalSerializer());
        }

        public static Ghost.IAgent CreateAgent(IProtocol protocol, IStreamable stream)
        {
            return new Ghost.Agent(stream, protocol, new Regulus.Remote.Serializer(protocol.SerializeTypes) , new Regulus.Remote.InternalSerializer());
        }

        public static TcpIntegral CreateTcp(IProtocol protocol, ISerializable serializable)
        {
            var connecter = new Regulus.Network.Tcp.Connecter();
            return new TcpIntegral(connecter, CreateAgent(protocol, connecter, serializable));
        }

        public static TcpIntegral CreateTcp(IProtocol protocol)
        {
            var connecter = new Regulus.Network.Tcp.Connecter();
            return new TcpIntegral(connecter, CreateAgent(protocol, connecter, new Regulus.Remote.Serializer(protocol.SerializeTypes)));
        }
    }
}