namespace PinionCore.Remote.Tools.Protocol.Sources.TestCommon
{
    public class EchoTester : Echoable
    {
        readonly int _Val;
        public EchoTester(int val = 0)
        {
            _Val = val;
        }
        public PinionCore.Remote.Value<int> Echo()
        {            
            return _Val;
        }
    }
}
