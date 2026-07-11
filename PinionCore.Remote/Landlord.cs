namespace PinionCore.Remote
{
    internal class LongProvider : ILandlordProviable<long>
    {


        long _Current;
        public LongProvider()
        {
            _Current = 0;
        }



        long ILandlordProviable<long>.Spawn()
        {
            // Rent 會從多執行緒進入(client 端 stream RPC 走 IO 執行緒);
            // 停用回收後 Spawn 成為熱路徑,必須原子遞增避免撞號
            return System.Threading.Interlocked.Increment(ref _Current);
        }
    }
    public class Landlord<T>
    {

        readonly System.Collections.Concurrent.ConcurrentBag<T> _Enrollments;

        readonly ILandlordProviable<T> _Provider;
        public Landlord(ILandlordProviable<T> provider)
        {
            _Provider = provider;
            _Enrollments = new System.Collections.Concurrent.ConcurrentBag<T>();
        }

        public T Rent()
        {
            if (_Enrollments.TryTake(out T id))
            {
                return id;
            }

            return _Provider.Spawn();


        }
        public void Return(T obj)
        {
            //_Enrollments.Add(obj);
        }

    }



}
