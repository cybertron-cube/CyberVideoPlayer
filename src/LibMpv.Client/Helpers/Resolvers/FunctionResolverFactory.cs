using LibMpv.Client.Native;

namespace LibMpv.Client;

public static class FunctionResolverFactory
{
    public static IFunctionResolver Create()
    {
        if (OperatingSystem.IsWindows())
            return new WindowsFunctionResolver();
        if (OperatingSystem.IsLinux())
            return new LinuxFunctionResolver();
        if (OperatingSystem.IsMacOS())
            return new MacFunctionResolver();
        throw new PlatformNotSupportedException();
    }
}
