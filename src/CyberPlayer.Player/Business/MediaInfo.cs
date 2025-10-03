using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reactive;
using System.Reactive.Subjects;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CyberPlayer.Player.AppSettings;
// ReSharper disable InconsistentNaming

namespace CyberPlayer.Player.Business;

public class MediaInfo : IDisposable
{
    public enum StreamKind
    {
        General,
        Video,
        Audio,
        Text,
        Other,
        Image,
        Menu,
    }

    public enum InfoKind
    {
        Name,
        Text,
        Measure,
        Options,
        NameText,
        MeasureText,
        Info,
        HowTo
    }

    public enum InfoOptions
    {
        ShowInInform,
        Support,
        ShowInSupported,
        TypeOfValue
    }

    [Flags]
    public enum InfoFileOptions
    {
        FileOptionNothing      = 0x00,
        FileOptionNoRecursive  = 0x01,
        FileOptionCloseAll     = 0x02,
        FileOptionMax          = 0x04
    };

    [Flags]
    public enum Status
    {
        None        =       0x00,
        Accepted    =       0x01,
        Filled      =       0x02,
        Updated     =       0x04,
        Finalized   =       0x08,
    }
    
    private MediaInfoFunctions.MediaInfo_New MediaInfo_New { get; set; } = null!;
    private MediaInfoFunctions.MediaInfo_Delete MediaInfo_Delete { get; set; } = null!;
    private MediaInfoFunctions.MediaInfo_Open MediaInfo_Open { get; set; } = null!;
    private MediaInfoFunctions.MediaInfoA_Open MediaInfoA_Open { get; set; } = null!;
    private MediaInfoFunctions.MediaInfo_Open_Buffer_Init MediaInfo_Open_Buffer_Init { get; set; } = null!;
    private MediaInfoFunctions.MediaInfo_Open_Buffer_Continue MediaInfo_Open_Buffer_Continue { get; set; } = null!;
    private MediaInfoFunctions.MediaInfoA_Open_Buffer_Continue MediaInfoA_Open_Buffer_Continue { get; set; } = null!;
    private MediaInfoFunctions.MediaInfo_Open_Buffer_Continue_GoTo_Get MediaInfo_Open_Buffer_Continue_GoTo_Get { get; set; } = null!;
    private MediaInfoFunctions.MediaInfoA_Open_Buffer_Continue_GoTo_Get MediaInfoA_Open_Buffer_Continue_GoTo_Get { get; set; } = null!;
    private MediaInfoFunctions.MediaInfo_Open_Buffer_Finalize MediaInfo_Open_Buffer_Finalize { get; set; } = null!;
    private MediaInfoFunctions.MediaInfoA_Open_Buffer_Finalize MediaInfoA_Open_Buffer_Finalize { get; set; } = null!;
    private MediaInfoFunctions.MediaInfo_Close MediaInfo_Close { get; set; } = null!;
    private MediaInfoFunctions.MediaInfo_Inform MediaInfo_Inform { get; set; } = null!;
    private MediaInfoFunctions.MediaInfoA_Inform MediaInfoA_Inform { get; set; } = null!;
    private MediaInfoFunctions.MediaInfo_GetI MediaInfo_GetI { get; set; } = null!;
    private MediaInfoFunctions.MediaInfoA_GetI MediaInfoA_GetI { get; set; } = null!;
    private MediaInfoFunctions.MediaInfo_Get MediaInfo_Get { get; set; } = null!;
    private MediaInfoFunctions.MediaInfoA_Get MediaInfoA_Get { get; set; } = null!;
    private MediaInfoFunctions.MediaInfo_Option MediaInfo_Option { get; set; } = null!;
    private MediaInfoFunctions.MediaInfoA_Option MediaInfoA_Option { get; set; } = null!;
    private MediaInfoFunctions.MediaInfo_State_Get MediaInfo_State_Get { get; set; } = null!;
    private MediaInfoFunctions.MediaInfo_Count_Get MediaInfo_Count_Get { get; set; } = null!;

    public IObservable<object> FileOpened => _fileOpened;
    
    private readonly Subject<object> _fileOpened = new();
    private static readonly bool MustUseAnsi;
    private readonly IntPtr _handle;
    private IntPtr _lib;

    static MediaInfo() => MustUseAnsi = !OperatingSystem.IsWindows();

    [RequiresUnreferencedCode("")]
    [RequiresDynamicCode("")]
    public MediaInfo(Settings settings)
    {
        FindAndLoadLibrary(settings);
        InitializeFunctions();
        _handle = MediaInfo_New();
    }

    ~MediaInfo()
    {
        ReleaseUnmanagedResources();
    }
    
    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }
    
    public int OpenBufferInit(long fileSize, long fileOffset) =>
        (int)MediaInfo_Open_Buffer_Init(_handle, fileSize, fileOffset);

    public int OpenBufferContinue(IntPtr buffer, IntPtr bufferSize) =>
        (int)MediaInfo_Open_Buffer_Continue(_handle, buffer, bufferSize);

    public long OpenBufferContinueGoToGet() =>
        MediaInfo_Open_Buffer_Continue_GoTo_Get(_handle);

    public int OpenBufferFinalize() =>
        (int)MediaInfo_Open_Buffer_Finalize(_handle);

    public int StateGet() =>
        (int)MediaInfo_State_Get(_handle);

    public int CountGet(StreamKind streamKind, int streamNumber = -1) =>
        (int)MediaInfo_Count_Get(_handle, (IntPtr)streamKind, streamNumber);

    public int Open(string fileName)
    {
        if (!MustUseAnsi)
            return (int)MediaInfo_Open(_handle, fileName);
        
        var fileNamePtr = Marshal.StringToHGlobalAnsi(fileName);
        var toReturn = (int)MediaInfoA_Open(_handle, fileNamePtr);
        Marshal.FreeHGlobal(fileNamePtr);
        
        _fileOpened.OnNext(Unit.Default);
        
        return toReturn;
    }

    public async Task<int> OpenAsync(string fileName) =>
        await Task.Run(() => Open(fileName));
    
    public string? Inform() =>
        MustUseAnsi ? Marshal.PtrToStringAnsi(MediaInfoA_Inform(_handle, 0))
            : Marshal.PtrToStringUni(MediaInfo_Inform(_handle, 0));

    public string? Get(StreamKind streamKind, int streamNumber, string parameter, InfoKind kindOfInfo = InfoKind.Text, InfoKind kindOfSearch = InfoKind.Name)
    {
        if (!MustUseAnsi)
            return Marshal.PtrToStringUni(MediaInfo_Get(_handle, (IntPtr)streamKind, streamNumber, parameter,
                (IntPtr)kindOfInfo, (IntPtr)kindOfSearch));
        
        var parameterPtr = Marshal.StringToHGlobalAnsi(parameter);
        var toReturn = Marshal.PtrToStringAnsi(MediaInfoA_Get(_handle, (IntPtr)streamKind, streamNumber, parameterPtr,
            (IntPtr)kindOfInfo, (IntPtr)kindOfSearch));
        Marshal.FreeHGlobal(parameterPtr);
        return toReturn;
    }

    public T Get<T>(StreamKind streamKind, int streamNumber, string parameter, InfoKind kindOfInfo = InfoKind.Text, InfoKind kindOfSearch = InfoKind.Name) =>
        (T)Convert.ChangeType(Get(streamKind, streamNumber, parameter, kindOfInfo, kindOfSearch), typeof(T))!;

    public string? Option(string option, string value = "")
    {
        if (!MustUseAnsi)
            return Marshal.PtrToStringUni(MediaInfo_Option(_handle, option, value));
        
        var optionPtr = Marshal.StringToHGlobalAnsi(option);
        var valuePtr = Marshal.StringToHGlobalAnsi(value);
        var toReturn = Marshal.PtrToStringAnsi(MediaInfoA_Option(_handle, optionPtr, valuePtr));
        Marshal.FreeHGlobal(optionPtr);
        Marshal.FreeHGlobal(valuePtr);
        return toReturn;
    }

    public void Close() =>
        MediaInfo_Close(_handle);

    private static string GetOsFileName()
    {
        if (OperatingSystem.IsWindows())
            return "MediaInfo.dll";
        if (OperatingSystem.IsMacOS())
            return "libmediainfo.dylib";
        if (OperatingSystem.IsLinux())
            return "libmediainfo.so.0.0.0";
        throw new PlatformNotSupportedException();
    }
    
    private void ReleaseUnmanagedResources()
    {
        if (_handle != 0)
            MediaInfo_Close(_handle);
        NativeLibrary.Free(_lib);
    }
    
    private void FindAndLoadLibrary(Settings settings)
    {
        var libPath = File.Exists(settings.MediaInfoPath) ? settings.MediaInfoPath
            : Path.Combine(settings.MediaInfoPath, GetOsFileName());
        
        if (!File.Exists(libPath))
            throw new DllNotFoundException($"MediaInfo library could not be found at \"{libPath}\"");
        
        _lib = NativeLibrary.Load(libPath);
    }

    [RequiresUnreferencedCode("Calls System.Type.GetNestedTypes()")]
    [RequiresDynamicCode("Calls System.Runtime.InteropServices.Marshal.GetDelegateForFunctionPointer(nint, Type)")]
    private void InitializeFunctions()
    {
        var type = typeof(MediaInfoFunctions);
        var delegatesTypes = type.GetNestedTypes();

        var thisType = this.GetType();
        
        foreach (var delegateType in delegatesTypes)
        {
            var functionPtr = NativeLibrary.GetExport(_lib, delegateType.Name);
            var prop = thisType.GetProperty(delegateType.Name, BindingFlags.Instance | BindingFlags.NonPublic);
            var function = Marshal.GetDelegateForFunctionPointer(functionPtr, delegateType);
            prop!.SetValue(this, function);
        }
    }
}
