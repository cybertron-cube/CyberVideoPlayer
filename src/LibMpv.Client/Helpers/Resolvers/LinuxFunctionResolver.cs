using System.Runtime.InteropServices;

namespace LibMpv.Client.Native;

public class LinuxFunctionResolver : FunctionResolverBase
{
    private const string Libdl = "libdl.so.2";

    private const int RTLD_NOW = 0x002;

    protected override string GetNativeLibraryName(string libraryName, int version)
    {
        return File.Exists(Path.Combine(libmpv.RootPath, $"{libraryName}.so.{version}")) ?
            $"{libraryName}.so.{version}"
            : $"{libraryName}.so";
    }

    protected override IntPtr LoadNativeLibrary(string libraryName) => dlopen(libraryName, RTLD_NOW);

    protected override IntPtr FindFunctionPointer(IntPtr nativeLibraryHandle, string functionName) => dlsym(nativeLibraryHandle, functionName);


    [DllImport(Libdl)]
    public static extern IntPtr dlsym(IntPtr handle, string symbol);

    [DllImport(Libdl)]
    public static extern IntPtr dlopen(string fileName, int flag);
}
