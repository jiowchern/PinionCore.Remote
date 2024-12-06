﻿using System;
using System.Net;
using System.Net.Sockets;
using PinionCore.Utility;

namespace PinionCore.Network.Tcp
{
    public class Listener
    {
        private readonly System.Net.Sockets.Socket _Socket;
        private event Action<Peer> _AcceptEvent;

        public event Action<Peer> AcceptEvent
        {
            add { _AcceptEvent += value; }
            remove { _AcceptEvent -= value; }
        }
        public Listener()
        {
            _Socket = new System.Net.Sockets.Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _Socket.NoDelay = true;
        }
        public void Bind(int port)
        {
            Bind(port, 10);
        }
        public void Bind(int Port , int backlog)
        {
            _Socket.Bind(new IPEndPoint(IPAddress.Any, Port));
            
            _Socket.Listen(backlog);
            _Socket.BeginAccept(_Accept, state: null);
        }

        private void _Accept(IAsyncResult Ar)
        {
            try
            {
                System.Net.Sockets.Socket socket = _Socket.EndAccept(Ar);
                _AcceptEvent(new Peer(socket));
                _Socket.BeginAccept(_Accept, state: null);
            }
            catch (SocketException se)
            {
                Singleton<Log>.Instance.WriteInfo($"accept {se.ToString()}.");
            }
            catch (ObjectDisposedException ode)
            {
                Singleton<Log>.Instance.WriteInfo($"accept object disposed {ode.ToString()}.");
            }
            catch (InvalidOperationException ioe)
            {
                Singleton<Log>.Instance.WriteInfo($"accept invalid operation {ioe.ToString()}.");
            }
            catch (Exception e)
            {
                Singleton<Log>.Instance.WriteInfo($"accept {e.ToString()}.");
            }
        }

        public void Close()
        {
            if (_Socket.Connected)
                _Socket.Shutdown(SocketShutdown.Both);

            _Socket.Close();
        }
    }
}
