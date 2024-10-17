namespace PinionCore.Remote
{
    public interface Exchangeable<TReq,TRes>
    {
//        TReq[] RequestCodes { get; }
        void Request(TReq code, PinionCore.Memorys.Buffer args);
        event System.Action<TRes, PinionCore.Memorys.Buffer> ResponseEvent;
    }
    public interface ServerExchangeable: Exchangeable<ClientToServerOpCode , ServerToClientOpCode>
    {
        
    }

    public interface ClientExchangeable : Exchangeable<ServerToClientOpCode, ClientToServerOpCode>
    {

    }

    public static class ExchangeableExtension
    {
        public static readonly ServerExchangeable[] ServerEmpty = new ServerExchangeable[0];
        public static readonly ClientExchangeable[] ClientEmpty = new ClientExchangeable[0];
    }
}
