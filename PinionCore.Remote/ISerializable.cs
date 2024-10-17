namespace PinionCore.Remote
{
    public interface ISerializable
    {
        PinionCore.Memorys.Buffer Serialize(System.Type type, object instance);
        object Deserialize(System.Type type, PinionCore.Memorys.Buffer buffer);
    }
}