using PinionCore.Remote;
using PinionCore.Consoles.Chat1.Common;
using PinionCore.Utility;
using System;

namespace PinionCore.Consoles.Chat1.Client
{
    internal class ChatStatus : PinionCore.Utility.IStatus
    {
        private IPlayer _Player;
        
        private Command _Command;

        public ChatStatus(IPlayer player, Command command)
        {
            _Player = player;
            
            _Command = command;
        }

        void IStatus.Enter()
        {
            _Player.Chatters.Base.Supply += _AddChatter;
            _Player.Chatters.Base.Unsupply += _RemoveChatter;

            _RegistPlayer();
        }

        void IStatus.Update()
        {

        }
        void IStatus.Leave()
        {
            _UnegistPlayer();
            _Player.Chatters.Base.Supply -= _AddChatter;
            _Player.Chatters.Base.Unsupply -= _RemoveChatter;
        }

        private void _UnegistPlayer()
        {
            _Command.Unregister("send");
            _Player.PublicMessageEvent -= _PublicMessage;
            _Player.PrivateMessageEvent -= _PrivateMessage;
            _Player.AnnounceEvent -= _AnnounceMessage;
        }


        private void _RegistPlayer()
        {
            _Command.Register<string>("send", msg => _Player.Send(msg));
            _Player.PublicMessageEvent += _PublicMessage;
            _Player.PrivateMessageEvent += _PrivateMessage;
            _Player.AnnounceEvent += _AnnounceMessage;
        }

        private void _PrivateMessage(Common.Message msg)
        {
            System.Console.WriteLine($"[private]{msg.Name}:{msg.Context}");
        }

        private void _PublicMessage(Common.Message msg)
        {
            System.Console.WriteLine($"[normal]{msg.Name}:{msg.Context}");
        }
        private void _AnnounceMessage(Message msg)
        {
            System.Console.WriteLine($"[announce]{msg.Name}:{msg.Context}");
        }

        private void _RemoveChatter(IChatter cahtter)
        {
            _Command.Unregister($"send-{cahtter.Name.Value}");
        }

        private void _AddChatter(IChatter cahtter)
        {
            _Command.Register<string>($"send-{cahtter.Name.Value}", msg => cahtter.Whisper(msg));
        }
    }
}
