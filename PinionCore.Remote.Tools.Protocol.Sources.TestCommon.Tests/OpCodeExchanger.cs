using System;
using System.Linq;
using PinionCore.Memorys;
namespace PinionCore.Remote.Tools.Protocol.Sources.TestCommon.Tests
{
    public class OpCodeExchanger : ServerExchangeable
    {
        public readonly System.Collections.Generic.Queue<System.Tuple<ClientToServerOpCode, byte[]>> Requests;
        public OpCodeExchanger()
        {
            Requests = new System.Collections.Generic.Queue<Tuple<ClientToServerOpCode, byte[]>>();
        }
        public Action<ServerToClientOpCode, PinionCore.Memorys.Buffer> Responser; 
        event Action<ServerToClientOpCode, PinionCore.Memorys.Buffer> Exchangeable<ClientToServerOpCode, ServerToClientOpCode>.ResponseEvent
        {
            add
            {
                Responser += value;
            }

            remove
            {
                Responser -= value;
            }
        }


        public event Action<ClientToServerOpCode, byte[]> RequestEvent;
        void Exchangeable<ClientToServerOpCode, ServerToClientOpCode>.Request(ClientToServerOpCode code, PinionCore.Memorys.Buffer args)
        {
            Requests.Enqueue(new Tuple<ClientToServerOpCode, byte[]>(code, args.ToArray()) );
        }
        public Tuple<ClientToServerOpCode, byte[]> IgnoreUntil(ClientToServerOpCode code)
        {
            Tuple<ClientToServerOpCode, byte[]> pkg;
            while (Requests.TryDequeue(out pkg))
            {
                if (pkg.Item1 != code)
                    continue;

                return pkg;
            }

            return null;
        }

    }
}