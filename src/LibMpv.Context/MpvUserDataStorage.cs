namespace LibMpv.Context;

public class MpvUserDataStorage
{
    // True if in use
    private readonly Dictionary<int, bool> _userData = new();
    private readonly object _lock = new();
    
    // Could be better but we will likely not use many async methods anyway
    public ulong GetAvailableUserData()
    {
        lock (_lock)
        {
            var i = 1;
            while (_userData.TryGetValue(i, out var result))
            {
                if (!result)
                {
                    _userData[i] = true;
                    return (ulong)i;
                }

                i++;
            }
            
            _userData.Add(i, true);
            return (ulong)i;
        }
    }

    public void FreeUserData(ulong userData)
    {
        lock (_lock)
        {
            _userData[(int)userData] = false;
        }
    }
}