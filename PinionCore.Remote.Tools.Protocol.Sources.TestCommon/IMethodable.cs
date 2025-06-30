namespace PinionCore.Remote.Tools.Protocol.Sources.TestCommon
{
    public interface IMethodable : IMethodable2
    {
        PinionCore.Remote.Value<int[]> GetValue0(int _1, string _2, float _3, double _4, decimal _5, System.Guid _6);

        int NotSupported();

        PinionCore.Remote.Value<IMethodable> GetValueSelf();

        PinionCore.Remote.Value MethodNoValue();
    }
}
