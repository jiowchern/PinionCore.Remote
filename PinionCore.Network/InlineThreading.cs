namespace PinionCore.Network
{
    /// <summary>
    /// 就地執行:工作與幫浦都在呼叫端的執行緒與 context 下直接跑。
    /// </summary>
    public class InlineThreading : IThreading
    {
        void IThreading.Dispatch(System.Action work)
        {
            work();
        }

        void IThreading.Launch(System.Action pump)
        {
            pump();
        }
    }
}
