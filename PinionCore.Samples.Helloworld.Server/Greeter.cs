using PinionCore.Samples.HelloWorld.Protocols;

namespace PinionCore.Samples.HelloWorld.Server
{
    class Greeter : IGreeter
    {
        PinionCore.Remote.Value<HelloReply> IGreeter.SayHello(HelloRequest request)
        {
            return new HelloReply() { Message = $"Hello {request.Name}." };
        }
    }
}
