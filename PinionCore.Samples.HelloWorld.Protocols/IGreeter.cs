namespace PinionCore.Samples.HelloWorld.Protocols
{
    public interface IGreeter
    {
        PinionCore.Remote.Value<HelloReply> SayHello(HelloRequest request);
    }
}
