using System;

namespace PinionCore.Remote.Exceptions
{
    public class UnregisteredProtocolInterfaceException : System.Exception
    {
        public readonly Type SoulType;

        public UnregisteredProtocolInterfaceException(Type soulType)
            : base($"Cannot bind soul: interface {soulType.FullName} has no type id in the protocol. A Spirit interface must inherit PinionCore.Remote.Protocolable or be a base interface of a Protocolable interface, then rebuild the protocol.")
        {
            SoulType = soulType;
        }
    }
}
