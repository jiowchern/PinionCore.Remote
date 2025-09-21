namespace PinionCore.Remote.Gateway.Servers 
{
    class IdProvider : ILandlordProviable<uint>
    {
        public readonly PinionCore.Remote.Landlord<uint> Landlord;
        uint _CurrentId = 0;
        
        public IdProvider() 
        {
            Landlord = new PinionCore.Remote.Landlord<uint>(this);
        }
        uint ILandlordProviable<uint>.Spawn()
        {
            return ++_CurrentId;
        }
    }
}
