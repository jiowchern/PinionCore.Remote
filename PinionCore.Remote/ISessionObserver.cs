namespace PinionCore.Remote
{

    public interface ISessionObserver
    {
        /// <summary>
        ///     zh-tw:如果客戶端連線成功系統會呼叫此方法並把Binder傳入。
        ///     en:When the client is connected successfully, the system will call this method and pass the Binder.
        /// </summary>
        /// <param name="binder"></param>
        void OnSessionOpened(ISessionBinder binder);

        void OnSessionClosed(ISessionBinder binder);


    }
}
