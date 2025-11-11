using PinionCore.Network;



namespace PinionCore.Remote.Gateway.Registrys
{
    public class Line
    {
        public readonly IStreamable Frontend;
        public readonly IStreamable Backend;

        public Line()
        {
            var stream = new Stream();
            Frontend = stream;            
            Backend = new PinionCore.Network.ReverseStream(stream);
        }
    }
}

