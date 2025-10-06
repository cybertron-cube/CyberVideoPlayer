namespace LibMpv.Context;

public static class ExtensionMethods
{
    public static T? GetValue<T>(this Dictionary<string, object?> dict, string key)
    {
        var result = dict.TryGetValue(key, out var val);
        
        if (result)
            return (T?)val;
        
        return default;
    }
}
