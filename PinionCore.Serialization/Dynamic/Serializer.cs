namespace PinionCore.Serialization.Dynamic
{
    public class Serializer : PinionCore.Serialization.Serializer
    {
        public Serializer() : this(new StandardFinder())
        {

        }
        public Serializer(ITypeFinder finder) : base(new DescriberBuilder(finder).Describers)
        {
        }

        public Serializer(PinionCore.Memorys.IPool pool) : base(new DescriberBuilder(new StandardFinder()).Describers, pool)
        {

        }
    }
}
