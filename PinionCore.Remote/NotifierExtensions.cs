namespace PinionCore.Remote
{
    public static class NotifierExtensions
    {
        /// <summary>
        ///     以介面 T 將通知來源曝光為 Notifier。
        ///     來源需可隱含轉型為 INotifier&lt;T&gt;（INotifier 為 covariant），
        ///     例如 Depot&lt;World&gt;.ToNotifier&lt;IWorld&gt;() 要求 World 實作 IWorld，違反時為編譯錯誤。
        /// </summary>
        public static Notifier<T> ToNotifier<T>(this INotifier<T> notifier) where T : class
        {
            return new Notifier<T>(notifier);
        }
    }
}
