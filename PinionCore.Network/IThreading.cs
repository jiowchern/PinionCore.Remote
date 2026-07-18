namespace PinionCore.Network
{
    /// <summary>
    /// host 的執行緒政策,由建構時注入:
    /// Dispatch 決定一件工作在哪條執行緒、什麼時機消化;
    /// Launch 決定非同步幫浦以什麼 context 啟動(影響其 await 續跑的落點)。
    /// </summary>
    public interface IThreading
    {
        void Dispatch(System.Action work);
        void Launch(System.Action pump);
    }
}
