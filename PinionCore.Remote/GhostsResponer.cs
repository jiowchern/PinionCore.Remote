using System;
using PinionCore.Remote.Packages;

namespace PinionCore.Remote
{
    namespace ProviderHelper
    {
        public class GhostsResponer
        {
            private readonly IInternalSerializable _InternalSerializer;
            private readonly GhostsHandler _GhostHandler;
            private readonly GhostsReturnValueHandler _ReturnValueHandler;
            private readonly PingHandler _PingHandler;
            private readonly GhostsOwner _ProviderManager;
            private readonly IProtocol _Protocol;

            public GhostsResponer(
                IInternalSerializable internalSerializer,
                GhostsHandler ghost_handler,
                GhostsReturnValueHandler returnValueHandler,
                PingHandler pingHandler,
                GhostsOwner ghostsProviderManager,
                IProtocol protocol)
            {
                _InternalSerializer = internalSerializer;
                _GhostHandler = ghost_handler;
                _ReturnValueHandler = returnValueHandler;
                _PingHandler = pingHandler;
                _Protocol = protocol;
                _ProviderManager = ghostsProviderManager;
            }

            public event System.Action<byte[], byte[]> VersionCodeErrorEvent;
            public void OnResponse(ServerToClientOpCode code, PinionCore.Memorys.Buffer args)
            {
                _GhostHandler.UpdateAutoRelease();
                if(code != ServerToClientOpCode.Ping)
                    PinionCore.Utility.Log.Instance.WriteInfoImmediate("OnResponse: " + code);   
                switch (code)
                {
                    case ServerToClientOpCode.Ping:
                        _PingHandler.HandlePingResponse();
                        break;

                    case ServerToClientOpCode.SetProperty:
                        var setPropertyData = (PackageSetProperty)_InternalSerializer.Deserialize(args);
                        _GhostHandler.UpdateSetProperty(setPropertyData.EntityId, setPropertyData.Property, setPropertyData.Value);
                        break;

                    case ServerToClientOpCode.InvokeEvent:
                        var invokeEventData = (PackageInvokeEvent)_InternalSerializer.Deserialize(args);
                        _GhostHandler.InvokeEvent(invokeEventData.EntityId, invokeEventData.Event, invokeEventData.HandlerId, invokeEventData.EventParams);
                        break;

                    case ServerToClientOpCode.ErrorMethod:
                        var errorMethodData = (PackageErrorMethod)_InternalSerializer.Deserialize(args);
                        _ReturnValueHandler.ErrorReturnValue(errorMethodData.ReturnTarget, errorMethodData.Method, errorMethodData.Message);
                        break;

                    case ServerToClientOpCode.ReturnValue:
                        var returnValueData = (PackageReturnValue)_InternalSerializer.Deserialize(args);
                        _ReturnValueHandler.SetReturnValue(returnValueData.ReturnTarget, returnValueData.ReturnValue);
                        break;

                    case ServerToClientOpCode.LoadSoulCompile:
                        var loadSoulCompileData = (PackageLoadSoulCompile)_InternalSerializer.Deserialize(args);
                        LoadSoulCompile(loadSoulCompileData.TypeId, loadSoulCompileData.EntityId, loadSoulCompileData.ReturnId);
                        break;

                    case ServerToClientOpCode.LoadSoul:
                        var loadSoulData = (PackageLoadSoul)_InternalSerializer.Deserialize(args);
                        _GhostHandler.LoadSoul(loadSoulData.TypeId, loadSoulData.EntityId, loadSoulData.ReturnType);
                        break;

                    case ServerToClientOpCode.UnloadSoul:
                        var unloadSoulData = (PackageUnloadSoul)_InternalSerializer.Deserialize(args);
                        _GhostHandler.UnloadSoul(unloadSoulData.EntityId);
                        break;

                    case ServerToClientOpCode.AddPropertySoul:
                        var addPropertySoulData = (PackagePropertySoul)_InternalSerializer.Deserialize(args);
                        _GhostHandler.AddPropertySoul(addPropertySoulData);
                        break;

                    case ServerToClientOpCode.RemovePropertySoul:
                        var removePropertySoulData = (PackagePropertySoul)_InternalSerializer.Deserialize(args);
                        _GhostHandler.RemovePropertySoul(removePropertySoulData);
                        break;

                    case ServerToClientOpCode.ProtocolSubmit:
                        var protocolSubmitData = (PackageProtocolSubmit)_InternalSerializer.Deserialize(args);
                        ProtocolSubmit(protocolSubmitData);
                        break;

                    default:
                        // 處理其他未定義的操作碼
                        break;
                }
            }

            private void LoadSoulCompile(int typeId, long entityId, long returnId)
            {
                MemberMap map = _Protocol.GetMemberMap();
                System.Type type = map.GetInterface(typeId);
                IProvider provider = _ProviderManager.QueryProvider(type);

                GhostResponseHandler handler = _GhostHandler.FindHandler(entityId);
                if (returnId != 0)
                {
                    IGhost ghost = handler.FindGhost();
                    _ReturnValueHandler.PopReturnValue(returnId, ghost);
                }
                else
                {
                    provider.Ready(entityId);
                }
            }

            private void ProtocolSubmit(PackageProtocolSubmit data)
            {
                
                if (Comparison(_Protocol.VersionCode, data.VersionCode))
                {
                    _PingHandler.HandlePingResponse();
                }
                else
                {
                    VersionCodeErrorEvent?.Invoke(_Protocol.VersionCode, data.VersionCode);
                }
            }

            private bool Comparison(byte[] code1, byte[] code2)
            {
                return new PinionCore.Utility.Comparer<byte>(code1, code2, (arg1, arg2) => arg1 == arg2).Same;
            }
        }

    }
}
