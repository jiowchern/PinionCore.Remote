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

        private readonly IResponseQueue _ResponseQueue;

        private readonly IInternalSerializable _InternalSerializer;
        private TaskAwaiter<List<Memorys.Buffer>> _ReadTask;

        // 非 Ping 請求的消化政策,由 host 於建構時注入;封包依到達序派發,單一消化點保證順序
        private readonly PinionCore.Network.IThreading _Threading;
        // 封包迴圈執行緒 Dispose 後,已派發但尚未執行的工作必須棄行
        private volatile bool _Disposed;

        public event System.Action ErrorEvent;

        // 讀取任務完成(有封包可處理)時觸發,讓宿主的更新迴圈立即醒來而不是等輪詢間隔
        public event System.Action DataArrivedEvent;

        public ISessionBinder Binder
        {
            get { return _SoulProvider; }
        }



        public User(PinionCore.Network.PackageReader reader, PinionCore.Network.PackageSender sender, IProtocol protocol, ISerializable serializable, IInternalSerializable internal_serializable, PinionCore.Memorys.IPool pool, PinionCore.Network.IThreading threading)
        {
            _Threading = threading;
            _InternalSerializer = internal_serializable;

            _Protocol = protocol;

            _Reader = reader;
            _Sender = sender;

            _SoulProvider = new SoulProvider(this, this, protocol, serializable, _InternalSerializer);
            _ResponseQueue = this;



        }

        void _Launch()
        {
            _ReadTask = _Reader.Read().GetAwaiter();
            _ReadTask.OnCompleted(_NotifyDataArrived);
            var pkg = new PinionCore.Remote.Packages.PackageProtocolSubmit();
            pkg.VersionCode = _Protocol.VersionCode;

            Memorys.Buffer buf = _InternalSerializer.Serialize(pkg);
            _ResponseQueue.Push(ServerToClientOpCode.ProtocolSubmit, buf);
        }




        private void _NotifyDataArrived()
        {
            if (_Disposed)
                return;
            DataArrivedEvent?.Invoke();
        }

        private void _ReadDone(List<PinionCore.Memorys.Buffer> buffers)
        {

            foreach (Memorys.Buffer buffer in buffers)
            {
                // 單一封包失敗不能中斷整批,否則後續封包遺失
                try
                {
                    var pkg = (Packages.RequestPackage)_InternalSerializer.Deserialize(buffer);
                    _InternalRequest(pkg);
                }
                catch (Exception e)
                {
                    Utility.Singleton<Utility.Log>.Instance.WriteInfo($"User _ReadDone error {e}");
                }
            }

        }

        void _Shutdown()
        {
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
                // fast path:回應不含狀態,在封包迴圈立即回彈,不經消化政策
                _ResponseQueue.Push(ServerToClientOpCode.Ping, PinionCore.Memorys.Pool.Empty);
                return;
            }

            _Threading.Dispatch(() =>
            {
                if (_Disposed)
                    return;
                // 單筆請求失敗不影響其他請求
                try
                {
                    _HandleRequest(package);
                }
                catch (Exception e)
                {
                    Utility.Singleton<Utility.Log>.Instance.WriteInfo($"User request error {e}. pkgCode:{package.Code}");
                }
            });
        }

        private void _HandleRequest(PinionCore.Remote.Packages.RequestPackage package)
        {
            if (package.Code == ClientToServerOpCode.Release)
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
                _ExternalRequest(package);
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
                // 先取結果並換新讀取任務再處理:處理途中拋例外時
                // 讀取幫浦才不會停在同一個已完成任務上重複執行同批封包
                List<Memorys.Buffer> buffers = _ReadTask.GetResult();
                if (buffers.Count == 0)
                {
                    // PackageReader 只在流結束(讀到 0)時回空批次:
                    // 通知宿主移除此 session,否則會在死流上空轉且 session 永不離開
                    Utility.Singleton<Utility.Log>.Instance.WriteInfo("User stream closed, session leaving.");
                    ErrorEvent?.Invoke();
                    return;
                }
                _ReadTask = _Reader.Read().GetAwaiter();
                _ReadTask.OnCompleted(_NotifyDataArrived);
                _ReadDone(buffers);
            }
        }

        void IDisposable.Dispose()
        {
            _Disposed = true;
            _Shutdown();

            IDisposable readerDispose = _Reader;
            readerDispose.Dispose();

            IDisposable senderDispose = _Sender;
            senderDispose.Dispose();
        }
    }
}
