using System;

using PinionCore.Memorys;

namespace PinionCore.Remote.Gateway.Backends
{
    class Serializer : PinionCore.Remote.Gateway.Serializer
    {
        public Serializer(IPool pool)
            : base(pool, new Type[]
            {
                typeof(ClientToServerPackage),
                typeof(ServerToClientPackage),
                typeof(OpCodeClientToServer),
                typeof(OpCodeServerToClient),
                typeof(uint),
                typeof(byte[]),
                typeof(byte)
            })
        {
        }
    }
}
