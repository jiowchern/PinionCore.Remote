namespace PinionCore.Remote
{
    public interface IInternalSerializable
    {
        PinionCore.Memorys.Buffer Serialize(object instance);
        object Deserialize(PinionCore.Memorys.Buffer buffer);
    }
    
    


}
