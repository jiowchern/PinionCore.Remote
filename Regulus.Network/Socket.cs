using Regulus.Network.Package;
using Regulus.Utility;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace Regulus.Network
{
    public class Socket : IUpdatable<Timestamp>
    {

        public event Action CloseEvent;
        private readonly Line _Line;

        private readonly StatusMachine<Timestamp> _Machine;


        private PeerStatus _Status;


        private readonly SegmentStream _Stream;
        public Action<int, SocketError> WriteDoneHandler;





        private Socket(Line line)
        {
            _Stream = new SegmentStream(new SocketMessage[0]);



            _Line = line;
            _Machine = new StatusMachine<Timestamp>();


        }
        public Socket(Line line, PeerListener listener) : this(line)
        {
            _Status = PeerStatus.Connecting;
            listener.DoneEvent += ToTransmission;
            listener.ErrorEvent += ToClose;
            _Machine.Push(listener);
        }

        public Socket(Line line, PeerConnecter connecter) : this(line)
        {
            _Status = PeerStatus.Connecting;
            connecter.DoneEvent += ToTransmission;
            _Machine.Push(connecter);
        }



        private void ToTransmission()
        {
            _Status = PeerStatus.Transmission;
            PeerTransmission stage = new PeerTransmission(_Line, _Stream);
            stage.DisconnectEvent += _Disconnect;
            _Machine.Push(stage);
        }






        bool IUpdatable<Timestamp>.Update(Timestamp Arg)
        {
            _Machine.Update(Arg);
            return true;
        }

        void IBootable.Launch()
        {

        }



        void IBootable.Shutdown()
        {
            _Machine.Termination();
        }




        public EndPoint EndPoint { get { return _Line.EndPoint; } }

        public IWaitableValue<int> Send(byte[] buffer, int offset, int count)
        {

            return System.Threading.Tasks.Task<int>.Run(() =>
            {

                _Line.WriteTransmission(buffer.Skip(offset).ToArray());
                return count;
            }).ToWaitableValue();

        }



        public IWaitableValue<int> Receive(byte[] buffer, int offset, int count)
        {
            var task = System.Threading.Tasks.Task<int>.Run(() =>
            {

                int readCount = 0;
                var r = new Regulus.Utility.AutoPowerRegulator(new PowerRegulator());
                while (readCount == 0)
                {
                    r.Operate();
                    readCount = _Stream.Read(buffer, offset, count);
                }
                return readCount;
            });
            return task.ToWaitableValue();


        }

        public PeerStatus Status { get { return _Status; } }

        public void Disconnect()
        {
            _Disconnect();
        }


        private void _Disconnect()
        {
            if (_Status != PeerStatus.Close)
            {
                _Status = PeerStatus.Disconnect;
                PeerDisconnecter stage = new PeerDisconnecter(_Line);
                stage.DoneEvent += ToClose;
                _Machine.Push(stage);
            }

        }

        private void ToClose()
        {
            CloseEvent();
            _Status = PeerStatus.Close;
            _Machine.Empty();
        }



        public void Break()
        {
            if (_Status != PeerStatus.Close)
                ToClose();
        }
    }
}