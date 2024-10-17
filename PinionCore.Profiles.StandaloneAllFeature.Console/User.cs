using PinionCore.Profiles.StandaloneAllFeature.Protocols;
using PinionCore.Remote;

namespace PinionCore.Profiles.StandaloneAllFeature.Server
{
    class User : PinionCore.Profiles.StandaloneAllFeature.Protocols.Featureable
    {
        public readonly IBinder Binder;
        private readonly ISoul _Soul;

        public User(IBinder binder)
        {
            Binder = binder;
            _Soul = Binder.Bind<PinionCore.Profiles.StandaloneAllFeature.Protocols.Featureable>(this);
        }

        Value<string> Featureable.Inc(string value)
        {
            string reversed = new string(value.Reverse().ToArray());
            return new Value<string>(reversed);
        }
    }
}
