// See https://aka.ms/new-console-template for more information
using System.CommandLine;
using System.CommandLine.Parsing;
using System;
using System.Net;
using PinionCore.Remote.Gateway.Hosts;
using PinionCore.Remote.Soul;

internal class Program
{
    private static int Main(string[] args)
    {
        var regOption = new Option<string?>("-r", new[] { "--provider-address" })
        {
            Description = "registry provider address"
        };

        var listenerOption = new Option<int?>("-l", new[] { "--listen-port" })
        {
            Description = "listen port."
        };

        

        var rootCommand = new RootCommand("PinionCore Chat1 server host");
        rootCommand.Add(regOption);
        rootCommand.Add(listenerOption);
        

        var parseResult = CommandLineParser.Parse(rootCommand, args, new ParserConfiguration());
        if (parseResult.Errors.Count > 0)
        {
            foreach (var error in parseResult.Errors)
            {
                System.Console.Error.WriteLine(error.Message);
            }

            return 1;
        }
        var regIp = parseResult.GetValue(regOption) ?? "";
        var listenPort = parseResult.GetValue(listenerOption) ?? 0;


        var registryConnectAddress = IPEndPoint.Parse(regIp);

        var registry = new PinionCore.Remote.Gateway.Registrys.ProviderRegistry();


        var gateway = new GatewayHostServiceHub(new RoundRobinGameLobbySelectionStrategy());
        var tcpListener = new PinionCore.Remote.Server.Tcp.Listener();
        IListenable listenable = tcpListener;

        listenable.StreamableLeaveEvent += gateway.Source.Leave;
        listenable.StreamableEnterEvent += gateway.Source.Join;
        registry.ProviderAddedEvent += gateway.Sink.Register;
        registry.ProviderRemovedEvent += gateway.Sink.Unregister;
        tcpListener.Bind(listenPort);
        registry.Connect(1, registryConnectAddress);


        // wait for input to end...
        Console.ReadLine();



        //disposes...
        registry.Dispose();
        registry.ProviderAddedEvent -= gateway.Sink.Register;
        registry.ProviderRemovedEvent -= gateway.Sink.Unregister;
        tcpListener.Close();
        listenable.StreamableLeaveEvent -= gateway.Source.Leave;
        listenable.StreamableEnterEvent -= gateway.Source.Join;

        return 0;
    }
}
