namespace PinionCore.Remote.Tools.Protocol.Sources.TestCommon
{
    public interface Echoable
    {
        PinionCore.Remote.Value<int> Echo(int value);
    }
}
