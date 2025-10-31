using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using PinionCore.Memorys;


namespace PinionCore.Remote.Soul
{
    public class User : IRequestQueue, IResponseQueue, Advanceable, IDisposable
    {
        public delegate void DisconnectCallback();



        private event InvokeMethodCallback _InvokeMethodEvent;
        private event InvokeStreamMethodCallback _InvokeStreamMethodEvent;

        private class MethodRequest
        {
            public long EntityId { get; set; }

            public int MethodId { get; set; }

            public long ReturnId { get; set; }

            public byte[][] MethodParams { get; set; }
        }

        private readonly IProtocol _Protocol;

        private readonly SoulProvider _SoulProvider;

        private readonly PinionCore.Network.PackageReader _Reader;
        private readonly PinionCore.Network.PackageSender _Sender;

        private readonly System.Collections.Concurrent.ConcurrentQueue<PinionCore.Remote.Packages.RequestPackage> _ExternalRequests;

        private readonly IResponseQueue _ResponseQueue;

        private readonly IInternalSerializable _InternalSerializer;
        private TaskAwaiter<List<Memorys.Buffer>> _ReadTask;

        public event System.Action ErrorEvent;
        public IBinder Binder
        {
            get { return _SoulProvider; }
        }



        public User(PinionCore.Network.PackageReader reader, PinionCore.Network.PackageSender sender, IProtocol protocol, ISerializable serializable, IInternalSerializable internal_serializable, PinionCore.Memorys.IPool pool)
        {

            _InternalSerializer = internal_serializable;

            _Protocol = protocol;

            _Reader = reader;
            _Sender = sender;

            _ExternalRequests = new System.Collections.Concurrent.ConcurrentQueue<PinionCore.Remote.Packages.RequestPackage>();

            _SoulProvider = new SoulProvider(this, this, protocol, serializable, _InternalSerializer);
            _ResponseQueue = this;



        }

        void _Launch()
        {
            _ReadTask = _Reader.Read().GetAwaiter();
            var pkg = new PinionCore.Remote.Packages.PackageProtocolSubmit();
            pkg.VersionCode = _Protocol.VersionCode;

            Memorys.Buffer buf = _InternalSerializer.Serialize(pkg);
            _ResponseQueue.Push(ServerToClientOpCode.ProtocolSubmit, buf);
        }




        private void _ReadDone(List<PinionCore.Memorys.Buffer> buffers)
        {

            foreach (Memorys.Buffer buffer in buffers)
            {

                var pkg = (Packages.RequestPackage)_InternalSerializer.Deserialize(buffer);
                _InternalRequest(pkg);
            }

        }

        void _Shutdown()
        {
            PinionCore.Remote.Packages.RequestPackage req;
            while (_ExternalRequests.TryDequeue(out req))
            {

            }
            Utility.Singleton<Utility.Log>.Instance.WriteInfo("User offline leave.");
        }

        event InvokeMethodCallback IRequestQueue.InvokeMethodEvent
        {
            add { _InvokeMethodEvent += value; }
            remove { _InvokeMethodEvent -= value; }
        }

        event InvokeStreamMethodCallback IRequestQueue.InvokeStreamMethodEvent
        {
            add { _InvokeStreamMethodEvent += value; }
            remove { _InvokeStreamMethodEvent -= value; }
        }


        void IResponseQueue.Push(ServerToClientOpCode cmd, PinionCore.Memorys.Buffer buffer)
        {
            _StartSend(cmd, buffer);
        }

        private void _StartSend(ServerToClientOpCode cmd, Memorys.Buffer buffer)
        {
            var pkg = new PinionCore.Remote.Packages.ResponsePackage
            {
                Code = cmd,
                Data = buffer.ToArray()
            };
            Memorys.Buffer buf = _InternalSerializer.Serialize(pkg);
            _Sender.Push(buf);
        }

        private void _ExternalRequest(PinionCore.Remote.Packages.RequestPackage package)
        {
            if (package.Code == ClientToServerOpCode.CallMethod)
            {

                var data = (PinionCore.Remote.Packages.PackageCallMethod)_InternalSerializer.Deserialize(package.Data.AsBuffer());
                MethodRequest request = _ToRequest(data.EntityId, data.MethodId, data.ReturnId, data.MethodParams);
                _InvokeMethodEvent(request.EntityId, request.MethodId, request.ReturnId, request.MethodParams);
            }
            else if (package.Code == ClientToServerOpCode.CallStreamMethod)
            {
                var data = (PinionCore.Remote.Packages.PackageCallStreamMethod)_InternalSerializer.Deserialize(package.Data.AsBuffer());
                _InvokeStreamMethodEvent?.Invoke(data.EntityId, data.MethodId, data.ReturnId, data.Buffer, data.Count);
            }
            else if (package.Code == ClientToServerOpCode.AddEvent)
            {
                var data = (PinionCore.Remote.Packages.PackageAddEvent)_InternalSerializer.Deserialize(package.Data.AsBuffer());
                _SoulProvider.AddEvent(data.Entity, data.Event, data.Handler);
            }
            else if (package.Code == ClientToServerOpCode.RemoveEvent)
            {
                var data = (PinionCore.Remote.Packages.PackageRemoveEvent)_InternalSerializer.Deserialize(package.Data.AsBuffer());
                _SoulProvider.RemoveEvent(data.Entity, data.Event, data.Handler);
            }
            else
            {
                throw new Exception($"_ExternalRequest invalid request code {package.Code}.");
            }
        }
        private void _InternalRequest(PinionCore.Remote.Packages.RequestPackage package)
        {

            if (package.Code == ClientToServerOpCode.Ping)
            {
                _ResponseQueue.Push(ServerToClientOpCode.Ping, PinionCore.Memorys.Pool.Empty);
            }
            else if (package.Code == ClientToServerOpCode.Release)
            {
                var data = (PinionCore.Remote.Packages.PackageRelease)_InternalSerializer.Deserialize(package.Data.AsBuffer());
                _SoulProvider.Unbind(data.EntityId);
            }
            else if (package.Code == ClientToServerOpCode.UpdateProperty)
            {
                var data = (PinionCore.Remote.Packages.PackageSetPropertyDone)_InternalSerializer.Deserialize(package.Data.AsBuffer());
                _SoulProvider.SetPropertyDone(data.EntityId, data.Property);
            }
            else
            {
                _ExternalRequests.Enqueue(package);
            }

        }

        private MethodRequest _ToRequest(long entity_id, int method_id, long return_id, byte[][] method_params)
        {
            return new MethodRequest
            {
                EntityId = entity_id,
                MethodId = method_id,
                MethodParams = method_params,
                ReturnId = return_id
            };
        }

        public void Launch()
        {
            _Launch();
        }

        public void Shutdown()
        {
            _Shutdown();
        }

        void Advanceable.Advance()
        {
            if (_ReadTask.IsCompleted)
            {
                _ReadDone(_ReadTask.GetResult());
                _ReadTask = _Reader.Read().GetAwaiter();
            }
            PinionCore.Remote.Packages.RequestPackage pkg;
            while (_ExternalRequests.TryDequeue(out pkg))
            {
                try
                {
                    _ExternalRequest(pkg);
                }
                catch (Exception e)
                {
                    throw new Exception($"User _ExternalRequest error {e.ToString()}. pkgCode:{pkg.Code}");
                }

            }
        }

        void IDisposable.Dispose()
        {
            _Shutdown();

            IDisposable readerDispose = _Reader;
            readerDispose.Dispose();

            IDisposable senderDispose = _Sender;
            senderDispose.Dispose();
        }
    }
}
