namespace PinionCore.Consoles.Chat1.Common
{
    public interface IChatter
    {   
        PinionCore.Remote.Property<string> Name { get; }
        void Whisper(string message);
    }
    
}

