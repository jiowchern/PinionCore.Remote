namespace PinionCore.Consoles.Chat1.Common
{
 
    public interface ILogin
    {
        PinionCore.Remote.Value<bool> Login(string name);        
    }
    
}

