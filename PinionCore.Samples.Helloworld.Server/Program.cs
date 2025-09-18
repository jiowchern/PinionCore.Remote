namespace PinionCore.Samples.HelloWorld.Server
{
    internal class Program
    {
        static void Main(string[] args)
        {
            int port = int.Parse(args[0]);

            var protocol = PinionCore.Samples.HelloWorld.Protocols.ProtocolCreator.Create();

            var entry = new Entry();

            var set = PinionCore.Remote.Server.Provider.CreateTcpService(entry, protocol);
            var listener = set.Listener;
            var service = set.Service;


            listener.Bind(port);
            System.Console.WriteLine($"start.");
            while (entry.Enable)
            {
                System.Threading.Thread.Sleep(0);
            }
            listener.Close();
            service.Dispose();

            System.Console.WriteLine($"Press any key to end.");
            System.Console.ReadKey();
        }
    }
}
