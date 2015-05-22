﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace Regulus.Remoting.Ghost.Native
{
    public partial class Agent 
    {
        public static int RequestQueueCount { get { return OnlineStage.RequestQueueCount; } }
        public static int ResponseQueueCount { get { return OnlineStage.ResponseQueueCount; } }

        public static int Fps { get { return IOHandler.Instance.Fps; } }
        public static float Power { get { return IOHandler.Instance.Power; } }


        class OnlineStage : Regulus.Utility.IStage, Regulus.Remoting.IGhostRequest
        {
            public event Action DoneEvent;

            Regulus.Remoting.Native.PackageReader _Reader;
            Regulus.Remoting.Native.PackageWriter _Writer;

            static object _LockRequest = new object();

            public static int RequestQueueCount { get; private set; }


            static object _LockResponse= new object();

            public static int ResponseQueueCount { get; private set; }
            PackageQueue _Sends;
            PackageQueue _Receives;            
            private System.Net.Sockets.Socket _Socket;

            volatile bool _Enable;
            
            public OnlineStage(System.Net.Sockets.Socket  socket)
            {
                _Socket = socket;
                _Reader = new Regulus.Remoting.Native.PackageReader();
                _Writer = new Regulus.Remoting.Native.PackageWriter();
                _Sends = new PackageQueue();
                _Receives = new PackageQueue();                
                
            }
            
            void Utility.IStage.Enter()
            {
                _Enable = true;
                _ReaderStart();
                _WriterStart();
                
            }

            

            void Utility.IStage.Leave()
            {
                
                _WriterStop();
                _ReaderStop();
                
                if (_Socket != null)
                {
                    _Socket.Close(1000);
                    _Socket = null;
                }
                if (_Enable != false)
                {
                    DoneEvent();
                    DoneEvent = null;
                }
            }

            

            void Utility.IStage.Update()
            {

                if (_Enable == false)
                {
                    DoneEvent();
                    DoneEvent = null;
                }
                    
                
            }
            

            void IGhostRequest.Request(byte code, Dictionary<byte, byte[]> args)
            {                
                
                
                lock(_LockRequest)
                {
                    _Sends.Enqueue(new Package() { Args = args, Code = code });
                    RequestQueueCount++;
                }
                    
            }
            
            

            private void _ReceivePackage(Package package)
            {
                lock (_LockResponse)
                {
                    _Receives.Enqueue(package);
                    ResponseQueueCount++;
                }
            }

            

            

            internal void Process(AgentCore core)
            {
                
                lock (_LockResponse)
                {
                    var pkgs = _Receives.DequeueAll();
                    ResponseQueueCount -= pkgs.Length;

                    foreach (var pkg in pkgs)
                    {
                        core.OnResponse(pkg.Code, pkg.Args);
                    }
                }
            }

            
            private void _WriterStart()
            {
                _Writer.ErrorEvent += _Disable;
                _Writer.CheckSourceEvent += _SendsPop;
                _Writer.Start(_Socket);
                
            }

            private void _WriterStop()
            {
                _Writer.ErrorEvent -= _Disable;
                _Writer.CheckSourceEvent -= _SendsPop;
                _Writer.Stop();
            }

            private Package[] _SendsPop()
            {
                lock (_LockRequest)
                {
                    var pkg = _Sends.DequeueAll();
                    RequestQueueCount -= pkg.Length;
                    return pkg;
                }
            }
            private void _ReaderStart()
            {
                _Reader.DoneEvent += _ReceivePackage;
                _Reader.ErrorEvent += _Disable;
                _Reader.Start(_Socket);                
            }

            private void _Disable()
            {
                _Enable = false;
            }
            
            private void _ReaderStop()
            {
                _Reader.DoneEvent -= _ReceivePackage;
                _Reader.ErrorEvent -= _Disable;
                _Reader.Stop();
            }
        }
    }
}