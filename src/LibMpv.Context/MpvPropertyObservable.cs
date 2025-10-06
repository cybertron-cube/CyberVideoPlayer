namespace LibMpv.Context;

public class MpvPropertyObservable<T> : IObservable<T>
{
    private readonly List<IObserver<T>> _observers = new();
    private readonly object _lock = new();
    private readonly MpvContext _mpv;
    private readonly ulong _mpvUserData;
    
    public MpvPropertyObservable(MpvContext mpv, ulong mpvUserData)
    {
        _mpv = mpv;
        _mpvUserData = mpvUserData;
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
            lock (_observable._lock)
            {
                _observable._observers.Remove(_observer);
                if (_observable._observers.Count == 0)
                {
                    _observable._mpv.UnobserveProperty(_observable._mpvUserData);
                    var propertyChangedObservables = _observable._mpv.PropertyChangedObservables;
                    // This could be better by already having the key passed in
                    var observableKey = propertyChangedObservables.Single(x => ReferenceEquals(x.Value, _observable))
                        .Key;
                    propertyChangedObservables.Remove(observableKey);
                }
            }
        }
    }
    
    public IDisposable Subscribe(IObserver<T> observer)
    {
        lock (_lock)
        {
            if (!_observers.Contains(observer))
                _observers.Add(observer);

            return new Unsubscriber(this, observer);
        }
    }

    internal void OnNextAll(T nextVal)
    {
        lock (_lock)
        {
            foreach (var observer in _observers)
            {
                observer.OnNext(nextVal);
            }
        }
    }
}
