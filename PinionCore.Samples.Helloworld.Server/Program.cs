using PinionCore.Remote.Server;

namespace PinionCore.Samples.HelloWorld.Server
{
    internal class Program
    {
        static async System.Threading.Tasks.Task Main(string[] args)
        {
            int port = int.Parse(args[0]);

            var protocol = PinionCore.Samples.HelloWorld.Protocols.ProtocolCreator.Create();
            var entry = new Entry();

            var soul = new PinionCore.Remote.Server.Host(entry, protocol);
            var service = (PinionCore.Remote.Soul.IService)soul;
            var (disposeServer, errorInfos) = await service.ListenAsync(
                new PinionCore.Remote.Server.Tcp.ListeningEndpoint(port, 10));

            foreach (var error in errorInfos)
            {
                System.Console.WriteLine($"Listener error: {error.Exception}");
                return;
            }

            System.Console.WriteLine("start.");
            while (entry.Enable)
            {
                System.Threading.Thread.Sleep(0);
            }

            disposeServer.Dispose();
            soul.Dispose();

            System.Console.WriteLine($"Press any key to end.");
            System.Console.ReadKey();
        }
    }
}
