namespace LibMpv.Context;

public class MpvPropertyObservable<T> : IObservable<T>
{
    protected readonly List<IObserver<T>> Observers = new();
    protected readonly MpvContext Mpv;
    protected readonly ulong MpvUserData;
    
    public MpvPropertyObservable(MpvContext mpv, ulong mpvUserData)
    {
        Mpv = mpv;
        MpvUserData = mpvUserData;
    }

    private class Unsubscriber : IDisposable
    {
        private readonly MpvPropertyObservable<T> _observable;
        private readonly IObserver<T> _observer;
        
        public Unsubscriber(MpvPropertyObservable<T> observable, IObserver<T> observer)
        {
            _observable = observable;
            _observer = observer;
        }

        public void Dispose()
        {
            _observable.Observers.Remove(_observer);
            if (_observable.Observers.Count == 0)
            {
                _observable.Mpv.UnobserveProperty(_observable.MpvUserData);
                var propertyChangedObservables = _observable.Mpv.PropertyChangedObservables;
                // This could be better by already having the key passed in
                var observableKey = propertyChangedObservables.Single(x => ReferenceEquals(x.Value, _observable)).Key;
                propertyChangedObservables.Remove(observableKey);
            }
        }
    }
    
    public IDisposable Subscribe(IObserver<T> observer)
    {
        if (!Observers.Contains(observer))
            Observers.Add(observer);

        return new Unsubscriber(this, observer);
    }

    internal void OnNextAll(T nextVal)
    {
        foreach (var observer in Observers)
        {
            observer.OnNext(nextVal);
        }
    }
}