namespace PinionCore.Profiles.StandaloneAllFeature.Protocols
{
    public interface Featureable
    {
        PinionCore.Remote.Value<string> Inc(string msg);
    }
}
