namespace PinionCore.Remote
{
    public class InternalSerializer : PinionCore.Remote.IInternalSerializable
    {
        private readonly PinionCore.Serialization.Serializer _Serializer;

        public InternalSerializer()
        {
            var essentialTypes = new System.Type[]
            {
                typeof(PinionCore.Remote.Packages.PackageProtocolSubmit),
                typeof(PinionCore.Remote.Packages.RequestPackage),
                typeof(PinionCore.Remote.Packages.ResponsePackage),
                typeof(PinionCore.Remote.Packages.PackageInvokeEvent),
                typeof(PinionCore.Remote.Packages.PackageErrorMethod),
                typeof(PinionCore.Remote.Packages.PackageReturnValue),
                typeof(PinionCore.Remote.Packages.PackageLoadSoulCompile),
                typeof(PinionCore.Remote.Packages.PackageLoadSoul),
                typeof(PinionCore.Remote.Packages.PackageUnloadSoul),
                typeof(PinionCore.Remote.Packages.PackageCallMethod),
                typeof(PinionCore.Remote.Packages.PackageRelease),
                typeof(PinionCore.Remote.Packages.PackageSetProperty),
                typeof(PinionCore.Remote.Packages.PackageSetPropertyDone),
                typeof(PinionCore.Remote.Packages.PackageAddEvent),
                typeof(PinionCore.Remote.Packages.PackageRemoveEvent),
                typeof(PinionCore.Remote.Packages.PackagePropertySoul),
                typeof(byte),
                typeof(byte[]),
                typeof(byte[][]),
                typeof(PinionCore.Remote.ClientToServerOpCode),
                typeof(PinionCore.Remote.ServerToClientOpCode),
                typeof(long),
                typeof(int),
                typeof(string),
                typeof(bool),
                typeof(char),
                typeof(char[])
            };
            _Serializer = new PinionCore.Serialization.Serializer(new PinionCore.Serialization.DescriberBuilder(essentialTypes).Describers);
        }

        object IInternalSerializable.Deserialize(PinionCore.Memorys.Buffer buffer)
        {
            return _Serializer.BufferToObject(buffer);
        }

        PinionCore.Memorys.Buffer IInternalSerializable.Serialize(object instance)
        {
            return _Serializer.ObjectToBuffer(instance);
        }
    }




}
