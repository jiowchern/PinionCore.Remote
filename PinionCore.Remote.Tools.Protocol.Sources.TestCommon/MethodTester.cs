using System;

namespace PinionCore.Remote.Tools.Protocol.Sources.TestCommon
{
    public class MethodTester : IMethodable
    {
        Value<int[]> IMethodable.GetValue0(int _1, string _2, float _3, double _4, decimal _5, Guid _6)
        {
            return new int[] {_1 };
        }

        Value<int> IMethodable1.GetValue1()
        {
            return 1;
        }

        Value<int> IMethodable2.GetValue2()
        {
            return 2;
        }

        Value<IMethodable> IMethodable.GetValueSelf()
        {
            return this;
        }

        int IMethodable.NotSupported()
        {
            return 0;
        }

        Value<HelloReply> IMethodable2.SayHello(HelloRequest request)
        {
            return new HelloReply() { Message = request.Name };
        }
    }
}
