using System;
using System.Collections.Generic;
using PinionCore.Memorys;
using PinionCore.Network;

namespace PinionCore.Remote.Gateway
{
    public class GatewaySessionConnector : IDisposable
    {
        private readonly PackageReader _Reader;
        private readonly PackageSender _Sender;
        private readonly PinionCore.Remote.Gateway.Serializer _Serializer;

        private System.Runtime.CompilerServices.TaskAwaiter<List<PinionCore.Memorys.Buffer>> _ReadTask;
        private bool _Started;

        private class Session
        {
            public uint Id;
            public Network.IStreamable Stream;
            public byte[] Buffer;
            public PinionCore.Remote.IAwaitable<int> Awaiter;
        }

        private readonly System.Collections.Generic.Dictionary<Network.IStreamable, Session> _Streams;
        private readonly System.Collections.Generic.Dictionary<uint, Session> _ById;
        private uint _NextId;

        public GatewaySessionConnector(PackageReader reader, PackageSender sender)
        {
            _Reader = reader;
            _Sender = sender;

            _Serializer = new PinionCore.Remote.Gateway.Serializer(
                PinionCore.Memorys.PoolProvider.Shared,
                new System.Type[]
                {
                    typeof(PinionCore.Remote.Gateway.Services.ClientToServerPackage),
                    typeof(PinionCore.Remote.Gateway.Services.ServerToClientPackage),
                    typeof(PinionCore.Remote.Gateway.Services.OpCodeClientToServer),
                    typeof(PinionCore.Remote.Gateway.Services.OpCodeServerToClient),
                    typeof(uint),
                    typeof(byte[]),
                    typeof(byte)
                }
            );

            _Started = false;
            _Streams = new System.Collections.Generic.Dictionary<Network.IStreamable, Session>();
            _ById = new System.Collections.Generic.Dictionary<uint, Session>();
            _NextId = 1;
        }

        public uint Join(Network.IStreamable stream)
        {
            var session = new Session
            {
                Id = _NextId++,
                Stream = stream,
                Buffer = new byte[4096]
            };
            _Streams.Add(stream, session);
            _ById.Add(session.Id, session);

            var pkg = new PinionCore.Remote.Gateway.Services.ClientToServerPackage
            {
                OpCode = PinionCore.Remote.Gateway.Services.OpCodeClientToServer.Join,
                Id = session.Id,
                Payload = new byte[0]
            };
            PinionCore.Memorys.Buffer buf = _Serializer.Serialize(pkg);
            _Sender.Push(buf);

            // start reading from application stream
            session.Awaiter = stream.Receive(session.Buffer, 0, session.Buffer.Length).GetAwaiter();
            return session.Id;
        }

        public void Leave(Network.IStreamable stream)
        {
            Session session;
            if (!_Streams.TryGetValue(stream, out session))
                return;

            var pkg = new PinionCore.Remote.Gateway.Services.ClientToServerPackage
            {
                OpCode = PinionCore.Remote.Gateway.Services.OpCodeClientToServer.Leave,
                Id = session.Id,
                Payload = new byte[0]
            };
            PinionCore.Memorys.Buffer buf = _Serializer.Serialize(pkg);
            _Sender.Push(buf);

            _Streams.Remove(stream);
            _ById.Remove(session.Id);
        }

        public void HandlePackages()
        {
            if (_Started == false)
            {
                _ReadTask = _Reader.Read().GetAwaiter();
                _Started = true;
            }

            // route server->client messages
            if (_ReadTask.IsCompleted)
            {
                List<PinionCore.Memorys.Buffer> buffers = _ReadTask.GetResult();
                foreach (PinionCore.Memorys.Buffer buffer in buffers)
                {
                    var obj = _Serializer.Deserialize(buffer);
                    var pkg = (PinionCore.Remote.Gateway.Services.ServerToClientPackage)obj;
                    Session session;
                    if (_ById.TryGetValue(pkg.Id, out session))
                    {
                        if (pkg.OpCode == PinionCore.Remote.Gateway.Services.OpCodeServerToClient.Message)
                        {
                            var payload = pkg.Payload ?? Array.Empty<byte>();
                            session.Stream.Send(payload, 0, payload.Length);
                        }
                    }
                }
                _ReadTask = _Reader.Read().GetAwaiter();
            }

            // send pending client->server messages
            foreach (Session s in _Streams.Values)
            {
                while (s.Awaiter != null && s.Awaiter.IsCompleted)
                {
                    var count = s.Awaiter.GetResult();
                    if (count > 0)
                    {
                        var payload = new byte[count];
                        Array.Copy(s.Buffer, 0, payload, 0, count);
                        var pkg = new PinionCore.Remote.Gateway.Services.ClientToServerPackage
                        {
                            OpCode = PinionCore.Remote.Gateway.Services.OpCodeClientToServer.Message,
                            Id = s.Id,
                            Payload = payload
                        };
                        PinionCore.Memorys.Buffer buf = _Serializer.Serialize(pkg);
                        _Sender.Push(buf);
                    }
                    // start next read
                    s.Awaiter = s.Stream.Receive(s.Buffer, 0, s.Buffer.Length).GetAwaiter();
                }
            }
        }

        void IDisposable.Dispose()
        {
        }
    }
}
