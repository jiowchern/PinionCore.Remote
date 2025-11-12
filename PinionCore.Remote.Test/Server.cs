using PinionCore.Remote;


namespace RemotingTest
{
    internal class Server : IEntry, ITestReturn, ITestGPI
    {
        private ISessionBinder _Binder;

        void ISessionObserver.OnSessionOpened(ISessionBinder binder)
        {
            binder.Return<ITestReturn>(this);
            _Binder = binder;
            _Binder.Bind<ITestGPI>(this);
        }

        void ISessionObserver.OnSessionClosed(ISessionBinder binder)
        {
            _Binder = null;
        }

        Value<int> ITestGPI.Add(int a, int b)
        {
            return a + b;
        }

        Value<ITestInterface> ITestReturn.Test(int a, int b)
        {
            return new TestInterface();
        }

        void IEntry.Update()
        {

        }
    }
}
