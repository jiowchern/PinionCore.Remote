// See https://aka.ms/new-console-template for more information
using PinionCore.Remote.Soul;

Console.WriteLine("Hello, World!");


//var agent = new PinionCore.Remote.Ghost.Agent();

var gateway = new PinionCore.Remote.Gateway.Servers.GatewayServerServiceHub();
var tcpListener = new PinionCore.Remote.Server.Tcp.Listener();
IListenable listenable = tcpListener;
listenable.StreamableLeaveEvent += gateway.Source.Leave;
listenable.StreamableEnterEvent += gateway.Source.Join;
tcpListener.Bind(53771);






//disposes...


tcpListener.Close();
listenable.StreamableLeaveEvent -= gateway.Source.Leave;
listenable.StreamableEnterEvent -= gateway.Source.Join;
