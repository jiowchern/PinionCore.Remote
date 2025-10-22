namespace PinionCore.Remote.Tools.Protocol.Sources.TestCommon
{
    namespace MultipleNotices
    {
        public class MultipleNotices : IMultipleNotices
        {

            public readonly Depot<INumber> Numbers1;
            public readonly Depot<INumber> Numbers2;

            Value<int> IMultipleNotices.GetNumber1Count()
            {
                return Numbers1.Items.Count;
            }

            Value<int> IMultipleNotices.GetNumber2Count()
            {
                return Numbers2.Items.Count;
            }

            Notifier<INumber> IMultipleNotices.Numbers1 => new Notifier<INumber>(Numbers1);

            Notifier<INumber> IMultipleNotices.Numbers2 => new Notifier<INumber>(Numbers2);



            public MultipleNotices()
            {

                Numbers1 = new Depot<INumber>();
                Numbers2 = new Depot<INumber>();
            }
        }
    }
}
