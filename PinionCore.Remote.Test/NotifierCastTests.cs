using NUnit.Framework;

namespace PinionCore.Remote.Test
{
    public interface INotifierCastControl
    {
    }

    public interface INotifierCastView
    {
    }

    public class NotifierCastItem : INotifierCastControl, INotifierCastView
    {
    }

    public class NotifierCastTests
    {
        [NUnit.Framework.Test]
        public void ToNotifierSupplyTest()
        {
            var depot = new PinionCore.Remote.Depot<NotifierCastItem>();
            PinionCore.Remote.Notifier<INotifierCastControl> notifier = depot.ToNotifier<INotifierCastControl>();
            ITypeObjectNotifiable notifiable = notifier;

            TypeObject supplied = null;
            notifiable.SupplyEvent += typeObject => supplied = typeObject;

            var instance = new NotifierCastItem();
            depot.Items.Add(instance);

            NUnit.Framework.Assert.AreSame(instance, supplied.Instance);
            NUnit.Framework.Assert.AreEqual(typeof(INotifierCastControl), supplied.Type);
        }

        [NUnit.Framework.Test]
        public void ToNotifierUnsupplyTest()
        {
            var depot = new PinionCore.Remote.Depot<NotifierCastItem>();
            PinionCore.Remote.Notifier<INotifierCastControl> notifier = depot.ToNotifier<INotifierCastControl>();
            ITypeObjectNotifiable notifiable = notifier;

            TypeObject unsupplied = null;
            notifiable.UnsupplyEvent += typeObject => unsupplied = typeObject;

            var instance = new NotifierCastItem();
            depot.Items.Add(instance);
            depot.Items.Remove(instance);

            NUnit.Framework.Assert.AreSame(instance, unsupplied.Instance);
        }

        [NUnit.Framework.Test]
        public void MultipleNotifiersFromSingleDepotTest()
        {
            // 一個 Depot 依實作的遠端介面產生多個 Notifier。
            var depot = new PinionCore.Remote.Depot<NotifierCastItem>();
            PinionCore.Remote.Notifier<INotifierCastControl> controlNotifier = depot.ToNotifier<INotifierCastControl>();
            PinionCore.Remote.Notifier<INotifierCastView> viewNotifier = depot.ToNotifier<INotifierCastView>();

            ITypeObjectNotifiable controlNotifiable = controlNotifier;
            ITypeObjectNotifiable viewNotifiable = viewNotifier;

            TypeObject controlSupplied = null;
            TypeObject viewSupplied = null;
            controlNotifiable.SupplyEvent += typeObject => controlSupplied = typeObject;
            viewNotifiable.SupplyEvent += typeObject => viewSupplied = typeObject;

            var instance = new NotifierCastItem();
            depot.Items.Add(instance);

            NUnit.Framework.Assert.AreSame(instance, controlSupplied.Instance);
            NUnit.Framework.Assert.AreEqual(typeof(INotifierCastControl), controlSupplied.Type);
            NUnit.Framework.Assert.AreSame(instance, viewSupplied.Instance);
            NUnit.Framework.Assert.AreEqual(typeof(INotifierCastView), viewSupplied.Type);
        }

        [NUnit.Framework.Test]
        public void ToNotifierDisposeTest()
        {
            var depot = new PinionCore.Remote.Depot<NotifierCastItem>();
            PinionCore.Remote.Notifier<INotifierCastControl> notifier = depot.ToNotifier<INotifierCastControl>();
            ITypeObjectNotifiable notifiable = notifier;

            var suppliedCount = 0;
            notifiable.SupplyEvent += typeObject => suppliedCount++;

            notifier.Dispose();
            depot.Items.Add(new NotifierCastItem());

            NUnit.Framework.Assert.AreEqual(0, suppliedCount);
        }
    }
}
