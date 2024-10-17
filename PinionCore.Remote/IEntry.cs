using System.Diagnostics;

namespace PinionCore.Remote
{    
    public interface IEntry : IBinderProvider
    {
        void Update();
    }
}
