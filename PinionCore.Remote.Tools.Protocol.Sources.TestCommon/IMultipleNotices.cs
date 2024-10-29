namespace PinionCore.Remote.Tools.Protocol.Sources.TestCommon
{
    namespace MultipleNotices
    {
        public interface IMultipleNotices
        {
            PinionCore.Remote.Value<int> GetNumber1Count();
            PinionCore.Remote.Value<int> GetNumber2Count();
            PinionCore.Remote.Notifier<INumber> Numbers1 { get; }
            PinionCore.Remote.Notifier<INumber> Numbers2 { get; }
        }
    }
}
