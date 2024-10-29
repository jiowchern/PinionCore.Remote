namespace PinionCore.Remote
{
    public interface IResponseQueue
    {
        void Push(ServerToClientOpCode code, PinionCore.Memorys.Buffer buffer);
    }
}
