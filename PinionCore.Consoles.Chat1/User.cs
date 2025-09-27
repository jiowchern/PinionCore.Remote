namespace PinionCore.Consoles.Chat1
{
    internal sealed class User
    {
        private readonly PinionCore.Remote.IBinder _binder;
        private readonly Room _room;
        private readonly PinionCore.Utility.StageMachine _machine;

        public User(PinionCore.Remote.IBinder binder, Room room)
        {
            _binder = binder;
            _room = room;
            _machine = new PinionCore.Utility.StageMachine();

            _ToLogin();
        }

        private void _ToLogin()
        {
            var stage = new UserLogin(_binder);
            stage.DoneEvent += _ToChat;
            _machine.Push(stage);
        }

        private void _ToChat(string name)
        {
            var stage = new UserChat(_binder, _room, name);
            stage.DoneEvent += _ToLogin;
            _machine.Push(stage);
        }

        public void Dispose()
        {
            _machine.Clean();
        }
    }
}
