using System.Net;
using PinionCore.Samples.HelloWorld.Protocols;
using System;

namespace PinionCore.Samples.HelloWorld.Client
{
    internal class Program
    {
        public static bool Enable = true;
        static void Main(string[] args)
        {
            _Run(args).Wait();
        }

        private static async Task _Run(string[] args)
        {
            var ip = IPAddress.Parse(args[0]);
            var port = int.Parse(args[1]);
            var protocolAsm = typeof(IGreeter).Assembly;
            var protocol = PinionCore.Samples.HelloWorld.Protocols.ProtocolCreator.Create();
            var set = PinionCore.Remote.Client.Provider.CreateTcpAgent(protocol);
            var tcp = set.Connector;
            var agent = set.Agent;
            var connectResult = await tcp.ConnectAsync(new IPEndPoint(ip, port)).ConfigureAwait(false);
            if (connectResult.Exception != null)
            {
                throw connectResult.Exception;
            }

            var peer = connectResult.Peer ?? throw new InvalidOperationException($"Connector returned null peer for {ip}:{port}.");
            agent.Enable(peer);
            agent.QueryNotifier<Protocols.IGreeter>().Supply += (greeter) =>
            {
                String user = "you";
                greeter.SayHello(new HelloRequest() { Name = user }).OnValue += _GetReply;
            };

            while (Enable)
            {
                System.Threading.Thread.Sleep(0);
                agent.HandleMessages();
                agent.HandlePackets();
            }

            await peer.Disconnect();
            agent.Disable();
            System.Console.WriteLine($"Press any key to end.");
            System.Console.ReadKey();
        }

        private static void _GetReply(HelloReply reply)
        {
            System.Console.WriteLine($"Receive message : {reply.Message}");
            Enable = false;
        }
    }
}
