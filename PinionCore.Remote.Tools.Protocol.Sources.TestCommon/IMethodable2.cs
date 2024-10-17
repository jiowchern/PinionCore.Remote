namespace PinionCore.Remote.Tools.Protocol.Sources.TestCommon
{
    public class HelloRequest
    {
        public string Name;
    }

    public class HelloReply
    {
        public string Message;
    }
    

    public interface IMethodable2 : IMethodable1
    {
        PinionCore.Remote.Value<int> GetValue2();
        PinionCore.Remote.Value<HelloReply> SayHello(HelloRequest request);
    }
}
