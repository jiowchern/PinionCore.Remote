using System.Collections.Generic;

namespace PinionCore.Remote
{
    public class ReturnValueQueue
    {
        private readonly IdLandlord _IdLandlord;
        private readonly Dictionary<long, IValue> _ReturnValues;
        private readonly Dictionary<long, string> _CallerStacks;

        // 開啟後每次帶回傳 RPC 會捕捉呼叫端堆疊,錯誤回應時可指出呼叫點;有配置成本,僅建議除錯環境開啟
        public bool CaptureCallerStack { get; set; }

        public ReturnValueQueue()
        {
            _IdLandlord = new IdLandlord();
            _ReturnValues = new Dictionary<long, IValue>();
            _CallerStacks = new Dictionary<long, string>();
        }

        internal IValue PopReturnValue(long returnTarget)
        {
            return PopReturnValue(returnTarget, out _);
        }

        internal IValue PopReturnValue(long returnTarget, out string callerStack)
        {
            if (_CallerStacks.TryGetValue(returnTarget, out callerStack))
                _CallerStacks.Remove(returnTarget);
            return _PopReturnValue(returnTarget);
        }

        public long PushReturnValue(IValue value)
        {
            var id = _IdLandlord.Rent();
            _ReturnValues.Add(id, value);
            if (CaptureCallerStack)
                _CallerStacks[id] = System.Environment.StackTrace;
            return id;
        }

        private IValue _PopReturnValue(long return_target)
        {
            IValue val = null;
            if (_ReturnValues.TryGetValue(return_target, out val))
            {
                _ReturnValues.Remove(return_target);
                _IdLandlord.Return(return_target);
            }

            return val;
        }
    }
}
