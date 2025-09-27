namespace PinionCore.Consoles.Chat1.Server
{
    internal sealed class Console : PinionCore.Utility.WindowConsole
    {
        private readonly Announceable _announcement;

        public Console(Announceable announcement)
        {
            _announcement = announcement;
        }

        protected override void _Launch()
        {
            Command.Register<string, string>("announce", Announce);
        }

        protected override void _Shutdown()
        {
            Command.Unregister("announce");
        }

        protected override void _Update()
        {
        }

        private void Announce(string name, string message)
        {
            _announcement.Announce(name, message);
        }
    }
}
