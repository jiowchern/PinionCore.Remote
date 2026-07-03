# Communications 
### Interface Notifier.
**The interface can broadcast its child interface to the client.**

Define the property```PinionCore.Remote.Notifier<T>```.  
```csharp
public interface IFoo
{
    PinionCore.Remote.Notifier<IBar> BarNotifier {get;}
}
```
The server implements the property.  
```csharp
namespace Server
{
    public interface IBar
    {
    }

    class Bar : IBar
    {    
    }

    class Foo : IFoo
    {
        readonly PinionCore.Remote.Depot<IBar> _Bars;
        readonly PinionCore.Remote.Notifier<IBar> _BarNotifier;

        public Foo()
        {
            _Bars = new PinionCore.Remote.Depot<IBar>();
            _BarNotifier = new PinionCore.Remote.Notifier<IBar>(_Bars);            
        }

        PinionCore.Remote.Notifier<IBar> IFoo.BarNotifier => _BarNotifier;        

        public void AddBar(Bar bar)
        {
            _Bars.Items.Add(bar);            
        }

        public void RemoveBar(Bar bar)
        {
            _Bars.Items.Remove(bar);            
        }    

        public void Dispose()
        {
            _Bars.Items.Clear();
            _BarNotifier.Dispose();
        }            
    }    
}
```
In the client.
```csharp
namespace Client
{
    class Program
    {
        void _OnFoo(IFoo foo)
        {
            foo.BarNotifier.Base.Supply += _OnBar;
        }
        void _OnBar(IBar bar)   
        {
            
        }
    }
}
```

---
### Exposing one concrete collection through multiple interfaces.

`INotifier<out T>` is covariant, so a depot of a concrete type can be exposed as a
notifier of any interface that type implements. Use `ToNotifier<T>()` — the
inheritance constraint is checked at compile time.

```csharp
namespace Server
{
    class Bar : IBar, IBarView
    {
    }

    class Foo : IFoo
    {
        readonly PinionCore.Remote.Depot<Bar> _Bars;
        readonly PinionCore.Remote.Notifier<IBar> _BarNotifier;
        readonly PinionCore.Remote.Notifier<IBarView> _BarViewNotifier;

        public Foo()
        {
            _Bars = new PinionCore.Remote.Depot<Bar>();
            _BarNotifier = _Bars.ToNotifier<IBar>();
            _BarViewNotifier = _Bars.ToNotifier<IBarView>();
        }

        PinionCore.Remote.Notifier<IBar> IFoo.BarNotifier => _BarNotifier;
        PinionCore.Remote.Notifier<IBarView> IFoo.BarViewNotifier => _BarViewNotifier;
    }
}
```

- One `Depot<Bar>` feeds any number of interface notifiers; adding or removing a
  `Bar` supplies/unsupplies every notifier created from the depot.
- `_Bars.ToNotifier<ISomethingBarDoesNotImplement>()` is a compile error.
- Create each notifier once and keep it in a field. Every `ToNotifier` call
  creates a new `Notifier<T>` that subscribes to the depot, so calling it inside
  a property getter leaks subscriptions.

---
#### Restrictions
1. Notifier supports only interfaces.
2. No duplicate instances may be added.
