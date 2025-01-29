namespace PinionCore.Remote.Tools.Protocol.Sources.TestCommon
{
    public class EchoTester : Echoable
    {
        public PinionCore.Remote.Value<int> Echo(int value)
        {
            return value;
        }
    }
}
