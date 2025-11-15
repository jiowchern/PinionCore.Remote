using System;
using System.Net;
using System.Threading.Tasks;
using PinionCore.Remote.Client;
using PinionCore.Samples.HelloWorld.Protocols;

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
            var ghost = new PinionCore.Remote.Client.Proxy(protocol);
            var agent = ghost.Agent;
            var endpoint = new PinionCore.Remote.Client.Tcp.ConnectingEndpoint(new IPEndPoint(ip, port));
            var connection = await agent.Connect(endpoint).ConfigureAwait(false);
            agent.QueryNotifier<Protocols.IGreeter>().Supply += (greeter) =>
            {
                string user = "you";
                greeter.SayHello(new HelloRequest() { Name = user }).OnValue += _GetReply;
            };

            while (Enable)
            {
                System.Threading.Thread.Sleep(0);
                agent.HandleMessages();
                agent.HandlePackets();
            }

            connection.Dispose();
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
