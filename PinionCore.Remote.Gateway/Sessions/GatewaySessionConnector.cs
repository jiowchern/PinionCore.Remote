using System;
using System.Collections.Generic;
using PinionCore.Memorys;
using PinionCore.Network;

namespace PinionCore.Remote.Gateway.Sessions
{
    class GatewaySessionConnector : IDisposable
    {
        private readonly PackageReader _Reader;
        private readonly PackageSender _Sender;
        private readonly PinionCore.Remote.Gateway.Sessions.Serializer _Serializer;

        private System.Runtime.CompilerServices.TaskAwaiter<List<Memorys.Buffer>> _ReadTask;
        private bool _Started;

        private class Session
        {
            public uint Id;
            public IStreamable Stream;
            public byte[] Buffer;
            public IAwaitable<int> Awaiter;
        }

        private readonly Dictionary<IStreamable, Session> _Streams;
        private readonly Dictionary<uint, Session> _ById;
        private uint _NextId;

        public GatewaySessionConnector(PackageReader reader, PackageSender sender , PinionCore.Remote.Gateway.Sessions.Serializer serializer)
        {
            _Reader = reader;
            _Sender = sender;

            _Serializer = serializer;

            _Started = false;
            _Streams = new Dictionary<IStreamable, Session>();
            _ById = new Dictionary<uint, Session>();
            _NextId = 1;
        }

        public uint Join(IStreamable stream)
        {
            var session = new Session
            {
                Id = _NextId++,
                Stream = stream,
                Buffer = new byte[4096]
            };
            _Streams.Add(stream, session);
            _ById.Add(session.Id, session);

            var pkg = new ClientToServerPackage
            {
                OpCode = OpCodeClientToServer.Join,
                Id = session.Id,
                Payload = new byte[0]
            };
            Memorys.Buffer buf = _Serializer.Serialize(pkg);
            _Sender.Push(buf);

            // start reading from application stream
            session.Awaiter = stream.Receive(session.Buffer, 0, session.Buffer.Length).GetAwaiter();
            return session.Id;
        }

        public void Leave(IStreamable stream)
        {
            Session session;
            if (!_Streams.TryGetValue(stream, out session))
                return;

            var pkg = new ClientToServerPackage
            {
                OpCode = OpCodeClientToServer.Leave,
                Id = session.Id,
                Payload = new byte[0]
            };
            Memorys.Buffer buf = _Serializer.Serialize(pkg);
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
                List<Memorys.Buffer> buffers = _ReadTask.GetResult();
                foreach (Memorys.Buffer buffer in buffers)
                {
                    var obj = _Serializer.Deserialize(buffer);
                    var pkg = (ServerToClientPackage)obj;
                    Session session;
                    if (_ById.TryGetValue(pkg.Id, out session))
                    {
                        if (pkg.OpCode == OpCodeServerToClient.Message)
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
                        var pkg = new ClientToServerPackage
                        {
                            OpCode = OpCodeClientToServer.Message,
                            Id = s.Id,
                            Payload = payload
                        };
                        Memorys.Buffer buf = _Serializer.Serialize(pkg);
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
