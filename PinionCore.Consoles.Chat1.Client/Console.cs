using PinionCore.Consoles.Chat1.Common;
using PinionCore.Remote.Ghost;

namespace PinionCore.Consoles.Chat1.Client
{
    internal abstract class Console : PinionCore.Utility.WindowConsole
    {
        private readonly PinionCore.Utility.StatusMachine _machine;
        protected Console(PinionCore.Remote.Ghost.IAgent agent)
        {
            Agent = agent;
            _machine = new PinionCore.Utility.StatusMachine();
        }

        protected PinionCore.Remote.Ghost.IAgent Agent { get; }

        protected override void _Launch()
        {
            Agent.QueryNotifier<IPlayer>().Supply += OnPlayerSupplied;
            Agent.QueryNotifier<ILogin>().Supply += OnLoginSupplied;
        }

        protected override void _Shutdown()
        {
            Agent.QueryNotifier<ILogin>().Supply -= OnLoginSupplied;
            Agent.QueryNotifier<IPlayer>().Supply -= OnPlayerSupplied;
            _machine.Termination();
        }

        private void OnLoginSupplied(ILogin login)
        {
            var status = new LoginStatus(login, Command);
            _machine.Push(status);
        }

        private void OnPlayerSupplied(IPlayer player)
        {
            var status = new ChatroomStatus(player, Command);
            _machine.Push(status);
        }

        protected override void _Update()
        {            
            Agent.HandleMessages();
            Agent.HandlePackets();
            _machine.Update();
        }
    }
}
