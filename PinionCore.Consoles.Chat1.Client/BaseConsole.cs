using PinionCore.Network.Tcp;

namespace PinionCore.Consoles.Chat1.Client
{
    class BaseConsole : PinionCore.Utility.WindowConsole
    {
        readonly PinionCore.Utility.StatusMachine _Machine;
        public BaseConsole() 
        {
            _Machine = new PinionCore.Utility.StatusMachine();
        }

        protected override void _Launch()
        {
            _ToConnect();
        }

        protected override void _Shutdown()
        {
            _Machine.Termination();
        }

        protected override void _Update()
        {
            _Machine.Update();
        }
        void _ToConnect()
        {
            var state = new ConnectState(Command);
            state.DoneEvent += _ToChat;
            _Machine.Push(state);
        }

        private void _ToChat(Peer peer)
        {
            var state = new ChatState(peer , Command);
            state.DoneEvent += _ToConnect;
            _Machine.Push(state);
        }
    }
}
