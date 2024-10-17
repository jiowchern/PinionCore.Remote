namespace PinionCore.Remote
{
    internal interface IAccessable
    {
        void Set(object value);
        object Get();
    }
}