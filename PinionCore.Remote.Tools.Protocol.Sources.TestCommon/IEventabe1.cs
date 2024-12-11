namespace PinionCore.Remote.Tools.Protocol.Sources.TestCommon
{
    public interface IEventabe1Test
    {
        event System.Action Event1;
        event System.Action Event2;
        event System.Action Event23;

    }
    public interface IEventabe1
    {
        event System.Action Event1;
        event System.Action<int> Event2;


    }
}
