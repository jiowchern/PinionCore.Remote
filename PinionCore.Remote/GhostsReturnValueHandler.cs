using System;
using PinionCore.Memorys;

namespace PinionCore.Remote
{
    namespace ProviderHelper
    {
        public class GhostsReturnValueHandler
        {
            private readonly ReturnValueQueue _ReturnValueQueue;
            private readonly ISerializable _Serializer;
            private readonly System.Collections.Generic.Dictionary<long, StreamContext> _StreamContexts;

            public event Action<string, string> ErrorMethodEvent;

            public GhostsReturnValueHandler(ISerializable serializer)
            {
                _ReturnValueQueue = new ReturnValueQueue();
                _Serializer = serializer;
                _StreamContexts = new System.Collections.Generic.Dictionary<long, StreamContext>();
            }

            public void SetReturnValue(long returnTarget, byte[] returnValue)
            {
                IValue value = _ReturnValueQueue.PopReturnValue(returnTarget);
                if (value == null)
                {
                    throw new Exception("SetReturnValue return value not found");
                }
                var returnInstance = _Serializer.Deserialize(value.GetObjectType(), returnValue.AsBuffer());
                value.SetValue(returnInstance);
            }

            public void ErrorReturnValue(long returnTarget, string method, string message)
            {
                _ReturnValueQueue.PopReturnValue(returnTarget);
                _StreamContexts.Remove(returnTarget);
                ErrorMethodEvent?.Invoke(method, message);
            }
            public void PopReturnValue(long return_id, IGhost ghost)
            {
                IValue value = _ReturnValueQueue.PopReturnValue(return_id);
                if (value != null)
                {
                    value.SetValue(ghost);
                }
            }


            public IValue GetReturnValue(long returnId)
            {
                return _ReturnValueQueue.PopReturnValue(returnId);
            }

            internal long PushReturnValue(IValue return_value)
            {
                return _ReturnValueQueue.PushReturnValue(return_value);
            }

            internal long PushStreamReturn(Value<int> returnValue, byte[] buffer, int offset)
            {
                if (returnValue == null)
                    throw new System.ArgumentNullException(nameof(returnValue));
                if (buffer == null)
                    throw new System.ArgumentNullException(nameof(buffer));
                if (offset < 0 || offset > buffer.Length)
                    throw new System.ArgumentOutOfRangeException(nameof(offset));

                long id = _ReturnValueQueue.PushReturnValue(returnValue);
                _StreamContexts[id] = new StreamContext(buffer, offset);
                return id;
            }

            public void CompleteStreamReturn(long returnTarget, int processCount, byte[] data)
            {
                if (!_StreamContexts.TryGetValue(returnTarget, out StreamContext context))
                {
                    throw new System.Exception("Stream return context not found.");
                }
                _StreamContexts.Remove(returnTarget);

                if (data != null && context.Buffer != null)
                {
                    int copyLength = data.Length;
                    if (processCount >= 0 && copyLength > processCount)
                    {
                        copyLength = processCount;
                    }
                    int remain = context.Buffer.Length - context.Offset;
                    if (copyLength > remain)
                    {
                        copyLength = remain;
                    }
                    if (copyLength > 0)
                    {
                        System.Buffer.BlockCopy(data, 0, context.Buffer, context.Offset, copyLength);
                    }
                }

                IValue value = _ReturnValueQueue.PopReturnValue(returnTarget);
                if (value == null)
                {
                    throw new System.Exception("Stream return value not found.");
                }
                value.SetValue(processCount);
            }

            private readonly struct StreamContext
            {
                public StreamContext(byte[] buffer, int offset)
                {
                    Buffer = buffer;
                    Offset = offset;
                }

                public byte[] Buffer { get; }
                public int Offset { get; }
            }
        }

    }
}
