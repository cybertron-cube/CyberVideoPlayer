using System;
using System.IO;
using System.Reactive;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CyberPlayer.Player.AppSettings;

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
    
    public MediaInfoFunctions.MediaInfo_New MediaInfo_New { get; private set; } = null!;
    public MediaInfoFunctions.MediaInfo_Delete MediaInfo_Delete { get; private set; } = null!;
    public MediaInfoFunctions.MediaInfo_Open MediaInfo_Open { get; private set; } = null!;
    public MediaInfoFunctions.MediaInfoA_Open MediaInfoA_Open { get; private set; } = null!;
    public MediaInfoFunctions.MediaInfo_Open_Buffer_Init MediaInfo_Open_Buffer_Init { get; private set; } = null!;
    public MediaInfoFunctions.MediaInfo_Open_Buffer_Continue MediaInfo_Open_Buffer_Continue { get; private set; } = null!;
    public MediaInfoFunctions.MediaInfoA_Open_Buffer_Continue MediaInfoA_Open_Buffer_Continue { get; private set; } = null!;
    public MediaInfoFunctions.MediaInfo_Open_Buffer_Continue_GoTo_Get MediaInfo_Open_Buffer_Continue_GoTo_Get { get; private set; } = null!;
    public MediaInfoFunctions.MediaInfoA_Open_Buffer_Continue_GoTo_Get MediaInfoA_Open_Buffer_Continue_GoTo_Get { get; private set; } = null!;
    public MediaInfoFunctions.MediaInfo_Open_Buffer_Finalize MediaInfo_Open_Buffer_Finalize { get; private set; } = null!;
    public MediaInfoFunctions.MediaInfoA_Open_Buffer_Finalize MediaInfoA_Open_Buffer_Finalize { get; private set; } = null!;
    public MediaInfoFunctions.MediaInfo_Close MediaInfo_Close { get; private set; } = null!;
    public MediaInfoFunctions.MediaInfo_Inform MediaInfo_Inform { get; private set; } = null!;
    public MediaInfoFunctions.MediaInfoA_Inform MediaInfoA_Inform { get; private set; } = null!;
    public MediaInfoFunctions.MediaInfo_GetI MediaInfo_GetI { get; private set; } = null!;
    public MediaInfoFunctions.MediaInfoA_GetI MediaInfoA_GetI { get; private set; } = null!;
    public MediaInfoFunctions.MediaInfo_Get MediaInfo_Get { get; private set; } = null!;
    public MediaInfoFunctions.MediaInfoA_Get MediaInfoA_Get { get; private set; } = null!;
    public MediaInfoFunctions.MediaInfo_Option MediaInfo_Option { get; private set; } = null!;
    public MediaInfoFunctions.MediaInfoA_Option MediaInfoA_Option { get; private set; } = null!;
    public MediaInfoFunctions.MediaInfo_State_Get MediaInfo_State_Get { get; private set; } = null!;
    public MediaInfoFunctions.MediaInfo_Count_Get MediaInfo_Count_Get { get; private set; } = null!;

    public IObservable<object> FileOpened => _fileOpened;
    
    private readonly Subject<object> _fileOpened = new();
    private static readonly bool MustUseAnsi;
    private readonly IntPtr _handle;
    private IntPtr _lib;

    static MediaInfo() => MustUseAnsi = !OperatingSystem.IsWindows();

    public MediaInfo(Settings settings)
    {
        FindAndLoadLibrary(settings);
        InitializeFunctions();
        try
        {
            _handle = MediaInfo_New();
        }
        catch
        {
            _handle = 0;
        }
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
    
    public int Open_Buffer_Init(long fileSize, long fileOffset)
    {
        return _handle == 0 ? 0 : (int)MediaInfo_Open_Buffer_Init(_handle, fileSize, fileOffset);
    }
    
    public int Open_Buffer_Continue(IntPtr buffer, IntPtr bufferSize)
    {
        return _handle == 0 ? 0 : (int)MediaInfo_Open_Buffer_Continue(_handle, buffer, bufferSize);
    }
    
    public long Open_Buffer_Continue_GoTo_Get()
    {
        return _handle == 0 ? 0 : MediaInfo_Open_Buffer_Continue_GoTo_Get(_handle);
    }
    
    public int Open_Buffer_Finalize()
    {
        return _handle == 0 ? 0 : (int)MediaInfo_Open_Buffer_Finalize(_handle);
    }

    public int State_Get()
    {
        return _handle == 0 ? 0 : (int)MediaInfo_State_Get(_handle);
    }

    public int Count_Get(StreamKind streamKind, int streamNumber = -1)
    {
        return _handle == 0 ? 0 : (int)MediaInfo_Count_Get(_handle, (IntPtr)streamKind, streamNumber);
    }
    
    public int Open(string fileName)
    {
        if (_handle == 0)
            return 0;
        
        if (!MustUseAnsi)
            return (int)MediaInfo_Open(_handle, fileName);
        
        var fileNamePtr = Marshal.StringToHGlobalAnsi(fileName);
        var toReturn = (int)MediaInfoA_Open(_handle, fileNamePtr);
        Marshal.FreeHGlobal(fileNamePtr);
        
        _fileOpened.OnNext(Unit.Default);
        
        return toReturn;
    }

    public async Task<int> OpenAsync(string fileName) => await Task.Run(() => Open(fileName));
    
    public string? Inform()
    {
        if (_handle == 0)
            return "Unable to load MediaInfo library";
        
        return MustUseAnsi ? Marshal.PtrToStringAnsi(MediaInfoA_Inform(_handle, 0))
            : Marshal.PtrToStringUni(MediaInfo_Inform(_handle, 0));
    }
    
    public string? Get(StreamKind streamKind, int streamNumber, string parameter, InfoKind kindOfInfo = InfoKind.Text, InfoKind kindOfSearch = InfoKind.Name)
    {
        if (_handle == 0)
            return "Unable to load MediaInfo library";
        
        if (!MustUseAnsi)
            return Marshal.PtrToStringUni(MediaInfo_Get(_handle, (IntPtr)streamKind, streamNumber, parameter,
                (IntPtr)kindOfInfo, (IntPtr)kindOfSearch));
        
        var parameterPtr = Marshal.StringToHGlobalAnsi(parameter);
        var toReturn = Marshal.PtrToStringAnsi(MediaInfoA_Get(_handle, (IntPtr)streamKind, streamNumber, parameterPtr,
            (IntPtr)kindOfInfo, (IntPtr)kindOfSearch));
        Marshal.FreeHGlobal(parameterPtr);
        return toReturn;
    }

    public T Get<T>(StreamKind streamKind, int streamNumber, string parameter, InfoKind kindOfInfo = InfoKind.Text, InfoKind kindOfSearch = InfoKind.Name)
    {
        return (T)Convert.ChangeType(Get(streamKind, streamNumber, parameter, kindOfInfo, kindOfSearch), typeof(T));
    }
    
    public string? Option(string option, string value = "")
    {
        if (_handle == 0)
            return "Unable to load MediaInfo library";
        
        if (!MustUseAnsi)
            return Marshal.PtrToStringUni(MediaInfo_Option(_handle, option, value));
        
        var optionPtr = Marshal.StringToHGlobalAnsi(option);
        var valuePtr = Marshal.StringToHGlobalAnsi(value);
        var toReturn = Marshal.PtrToStringAnsi(MediaInfoA_Option(_handle, optionPtr, valuePtr));
        Marshal.FreeHGlobal(optionPtr);
        Marshal.FreeHGlobal(valuePtr);
        return toReturn;
    }

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

    private void InitializeFunctions()
    {
        var type = typeof(MediaInfoFunctions);
        var delegatesTypes = type.GetNestedTypes();

        var thisType = this.GetType();
        
        foreach (var delegateType in delegatesTypes)
        {
            var functionPtr = NativeLibrary.GetExport(_lib, delegateType.Name);
            var prop = thisType.GetProperty(delegateType.Name);
            var function = Marshal.GetDelegateForFunctionPointer(functionPtr, delegateType);
            prop!.SetValue(this, function);
        }
    }
}