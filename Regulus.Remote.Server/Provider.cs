﻿namespace Regulus.Remote.Server
{

   
    public static class Provider
    {
        public static Soul.IService CreateService(IEntry entry, IProtocol protocol, Soul.IListenable listenable)
        {
            return new Soul.Service(entry, protocol, new Regulus.Remote.Serializer(protocol.SerializeTypes), listenable, new Regulus.Remote.InternalSerializer());
        }

        public static Soul.IService CreateService(IEntry entry,  IProtocol protocol, ISerializable serializable ,Soul.IListenable listenable)
        {
            return new Soul.Service(entry, protocol, serializable , listenable, new Regulus.Remote.InternalSerializer());
        }

        public static TcpIntegral CreateTcpService(IEntry entry, IProtocol protocol)
        {
            return CreateTcpService(entry , protocol,new Regulus.Remote.Serializer(protocol.SerializeTypes));
        }
        public static TcpIntegral CreateTcpService( IEntry entry, IProtocol protocol, ISerializable serializable)
        {
            var listener = new Regulus.Remote.Server.Tcp.Listener();
            var service = CreateService(entry, protocol, serializable, listener);
            return new TcpIntegral(listener , service);
        }
        public static WebIntegral CreateWebService(IEntry entry, IProtocol protocol)
        {
            return CreateWebService(entry, protocol, new Regulus.Remote.Serializer(protocol.SerializeTypes));
        }
        public static WebIntegral CreateWebService(IEntry entry, IProtocol protocol, ISerializable serializable)
        {
            var listener = new Regulus.Remote.Server.Web.Listener();
            var service = CreateService(entry, protocol, serializable, listener);
            return new WebIntegral(listener, service);
        }


    }
}
