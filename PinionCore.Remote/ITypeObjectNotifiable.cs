namespace PinionCore.Remote
{
    interface ITypeObjectNotifiable
    {

        event System.Action<TypeObject> SupplyEvent;
        event System.Action<TypeObject> UnsupplyEvent;
    }
}
