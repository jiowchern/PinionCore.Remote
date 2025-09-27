using PinionCore.Remote;
using PinionCore.Remote.Ghost;
using PinionCore.Consoles.Chat1.Common;
using PinionCore.Utility;
using System;

namespace PinionCore.Consoles.Chat1.Client
{    

    internal class ChatroomStatus : IStatus
    {

        readonly IPlayer _Player;
        
        private readonly Command _Command;
        
        public ChatroomStatus(IPlayer player, Command command)
        {
            _Player = player;        
            this._Command = command;
        }

        void IStatus.Enter()
        {

            _PlayerEnter(_Player);


        }

        void IStatus.Leave()
        {

            _PlayerLeave(_Player);
        }

        private void _PlayerLeave(IPlayer player)
        {
            _Command.Unregister("send");
            _Command.Unregister("exitroom");

            player.Chatters.Base.Supply -= _ChatterEnter;
            player.Chatters.Base.Unsupply -= _ChatterLeave;

            player.PrivateMessageEvent -= _PrivateMessage;
            player.PublicMessageEvent -= _PublicMessage;


        }

        private void _PlayerEnter(IPlayer player)
        {            

            player.PrivateMessageEvent += _PrivateMessage;
            player.PublicMessageEvent += _PublicMessage;

            player.Chatters.Base.Supply += _ChatterEnter;
            player.Chatters.Base.Unsupply += _ChatterLeave;

            _Command.Register<string>("send", player.Send);
            _Command.Register("exitroom", player.Quit);
        }

        private void _PublicMessage(Common.Message msg)
        {
            System.Console.WriteLine($"{msg.Name}:{msg.Context}");
        }

        private void _PrivateMessage(Common.Message msg)
        {
            System.Console.WriteLine($"[private]{msg.Name}:{msg.Context}");
        }

        private void _ChatterLeave(IChatter chatter)
        {
            _Command.Unregister($"to-{chatter.Name.Value}");
            System.Console.WriteLine($"{chatter.Name.Value} leave.");
            
        }

        private void _ChatterEnter(IChatter chatter)
        {
            _Command.Register<string>($"to-{chatter.Name.Value}", chatter.Whisper);
            System.Console.WriteLine($"{chatter.Name.Value} enter.");
        }

        void IStatus.Update()
        {
            
        }

        
    }
}
