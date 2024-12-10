using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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
        private TaskAwaiter<List<Memorys.Buffer>> _ReadTask;

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
            PinionCore.Utility.Log.Instance.WriteInfoImmediate("Agent online enter.");
            //Singleton<Log>.Instance.WriteInfo("Agent online enter.");
            _ReadTask = _Reader.Read().GetAwaiter();
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

            if(_ReadTask.IsCompleted)
            {
                _ReadDone(_ReadTask.GetResult());
                _ReadTask = _Reader.Read().GetAwaiter();
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
