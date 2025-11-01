using System;

namespace PinionCore.Remote.Gateway.Hosts
{
    class VersionKey : IEquatable<VersionKey>
    {
        private readonly string _versionString;

        public VersionKey(byte[] version)
        {
            _versionString = Convert.ToBase64String(version);
        }

        public bool Equals(VersionKey other) =>
            _versionString == other?._versionString;

        public override int GetHashCode() =>
            _versionString.GetHashCode();
        
    }
}


