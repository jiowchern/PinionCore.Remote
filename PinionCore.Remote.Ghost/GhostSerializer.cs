﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PinionCore.Memorys;
using PinionCore.Network;
using PinionCore.Utility;

namespace PinionCore.Remote.Ghost
{
    class GhostSerializer : ServerExchangeable
    {
        private readonly PinionCore.Network.PackageReader _Reader;
        private readonly PinionCore.Network.PackageSender _Sender;
        private readonly IInternalSerializable _Serializable;
        private readonly System.Collections.Concurrent.ConcurrentQueue<PinionCore.Remote.Packages.ResponsePackage> _Receives;


        private readonly System.Collections.Concurrent.ConcurrentBag<System.Exception> _Exceptions;
        public event System.Action<System.Exception> ErrorEvent;
        public GhostSerializer(PinionCore.Network.PackageReader reader, PackageSender sender, IInternalSerializable serializable)
        {
            _Exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();
            _Reader = reader;
            _Sender = sender;
            this._Serializable = serializable;

            _Receives = new System.Collections.Concurrent.ConcurrentQueue<PinionCore.Remote.Packages.ResponsePackage>();

            _ResponseEvent += _Empty;
        }

        private void _Empty(ServerToClientOpCode arg1, PinionCore.Memorys.Buffer arg2)
        {
        }

        event Action<ServerToClientOpCode, PinionCore.Memorys.Buffer> _ResponseEvent;

        event Action<ServerToClientOpCode, PinionCore.Memorys.Buffer> Exchangeable<ClientToServerOpCode, ServerToClientOpCode>.ResponseEvent
        {
            add
            {
                _ResponseEvent += value;

            }

            remove
            {
                _ResponseEvent -= value;
            }
        }

        void Exchangeable<ClientToServerOpCode, ServerToClientOpCode>.Request(ClientToServerOpCode code, PinionCore.Memorys.Buffer args)
        {
            Memorys.Buffer buf = _Serializable.Serialize(new PinionCore.Remote.Packages.RequestPackage()
            {
                Data = args.ToArray(),
                Code = code
            });
            _Sender.Push(buf);
        }

        public void Start()
        {
            Singleton<Log>.Instance.WriteInfo("Agent online enter.");
            Task.Run(async () => await _ReaderStart());
        }

        public void Stop()
        {

            _ReaderStop();

            PinionCore.Remote.Packages.ResponsePackage val2;
            while (_Receives.TryDequeue(out val2))
            {

            }
            Singleton<Log>.Instance.WriteInfo("Agent online leave.");
        }

        void _Update()
        {
            if (_Exceptions.TryTake(out Exception e))
            {
                ErrorEvent.Invoke(e);
                return;
            }
            _Process();
        }

        private void _Process()
        {


            while (_Receives.TryDequeue(out Packages.ResponsePackage receivePkg))
            {
                _ResponseEvent(receivePkg.Code, receivePkg.Data.AsBuffer());
            }
        }



        private async Task _ReaderStart()
        {

            List<Memorys.Buffer> packages = await _Reader.Read().ContinueWith(t =>
            {
                if (t.Exception != null)
                {
                    Singleton<Log>.Instance.WriteInfo($" Agent online error : {t.Exception}");
                    _Exceptions.Add(t.Exception);
                    return new List<PinionCore.Memorys.Buffer>();
                }
                return t.Result;
            });
            if (packages.Count == 0)
            {
                _Exceptions.Add(new System.Exception("Agent online error : read 0"));
                return;
            }
            _ReadDone(packages);
            await System.Threading.Tasks.Task.Delay(0).ContinueWith(t => _ReaderStart());
        }

        private void _ReadDone(List<Memorys.Buffer> buffers)
        {
            foreach (Memorys.Buffer buffer in buffers)
            {
                var pkg = (Packages.ResponsePackage)_Serializable.Deserialize(buffer);
                _Receives.Enqueue(pkg);
            }
        }

        private void _ReaderStop()
        {

        }

        public void Update()
        {
            _Update();
        }
    }
}
