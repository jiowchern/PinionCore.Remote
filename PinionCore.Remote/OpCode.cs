namespace PinionCore.Remote
{
    public enum ClientToServerOpCode : byte
    {
        CallMethod = 1,
        CallStreamMethod,

        Ping,

        Release,

        UpdateProperty,
        AddEvent,
        RemoveEvent,
    };

    public enum ServerToClientOpCode : byte
    {
        InvokeEvent = 1,

        LoadSoul,

        UnloadSoul,

        ReturnValue,
        ReturnStreamMethod,

        LoadSoulCompile,

        Ping,

        ErrorMethod,
        ProtocolSubmit,
        SetProperty,
        AddPropertySoul,
        RemovePropertySoul,

    }
}
