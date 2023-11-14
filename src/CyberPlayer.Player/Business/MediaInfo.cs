using System;
using System.Runtime.InteropServices;

namespace CyberPlayer.Player.Business;

public partial class MediaInfo : IDisposable
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
    
    [LibraryImport("mediainfo")]
    private static partial IntPtr MediaInfo_New();
    
    [LibraryImport("mediainfo")]
    private static partial void MediaInfo_Delete(IntPtr handle);
    
    [LibraryImport("mediainfo")]
    private static partial IntPtr MediaInfo_Open(IntPtr handle, [MarshalAs(UnmanagedType.LPWStr)] string fileName);
    
    [LibraryImport("mediainfo")]
    private static partial IntPtr MediaInfoA_Open(IntPtr handle, IntPtr fileName);
    
    [LibraryImport("mediainfo")]
    private static partial IntPtr MediaInfo_Open_Buffer_Init(IntPtr handle, long fileSize, long fileOffset);
    
    [LibraryImport("mediainfo")]
    private static partial IntPtr MediaInfoA_Open(IntPtr handle, long fileSize, long fileOffset);
    
    [LibraryImport("mediainfo")]
    private static partial IntPtr MediaInfo_Open_Buffer_Continue(IntPtr handle, IntPtr buffer, IntPtr bufferSize);
    
    [LibraryImport("mediainfo")]
    private static partial IntPtr MediaInfoA_Open_Buffer_Continue(IntPtr handle, long fileSize, byte[] buffer, IntPtr bufferSize);
    
    [LibraryImport("mediainfo")]
    private static partial long MediaInfo_Open_Buffer_Continue_GoTo_Get(IntPtr handle);
    
    [LibraryImport("mediainfo")]
    private static partial long MediaInfoA_Open_Buffer_Continue_GoTo_Get(IntPtr handle);
    
    [LibraryImport("mediainfo")]
    private static partial IntPtr MediaInfo_Open_Buffer_Finalize(IntPtr handle);
    
    [LibraryImport("mediainfo")]
    private static partial IntPtr MediaInfoA_Open_Buffer_Finalize(IntPtr handle);
    
    [LibraryImport("mediainfo")]
    private static partial void MediaInfo_Close(IntPtr handle);
    
    [LibraryImport("mediainfo")]
    private static partial IntPtr MediaInfo_Inform(IntPtr handle, IntPtr reserved);
    
    [LibraryImport("mediainfo")]
    private static partial IntPtr MediaInfoA_Inform(IntPtr handle, IntPtr reserved);
    
    [LibraryImport("mediainfo")]
    private static partial IntPtr MediaInfo_GetI(IntPtr handle, IntPtr streamKind, IntPtr streamNumber, IntPtr parameter, IntPtr kindOfInfo);
    
    [LibraryImport("mediainfo")]
    private static partial IntPtr MediaInfoA_GetI(IntPtr handle, IntPtr streamKind, IntPtr streamNumber, IntPtr parameter, IntPtr kindOfInfo);
    
    [LibraryImport("mediainfo")]
    private static partial IntPtr MediaInfo_Get(IntPtr handle, IntPtr streamKind, IntPtr streamNumber, [MarshalAs(UnmanagedType.LPWStr)] string parameter, IntPtr kindOfInfo, IntPtr kindOfSearch);
    
    [LibraryImport("mediainfo")]
    private static partial IntPtr MediaInfoA_Get(IntPtr handle, IntPtr streamKind, IntPtr streamNumber, IntPtr parameter, IntPtr kindOfInfo, IntPtr kindOfSearch);
    
    [LibraryImport("mediainfo")]
    private static partial IntPtr MediaInfo_Option(IntPtr handle, [MarshalAs(UnmanagedType.LPWStr)] string option, [MarshalAs(UnmanagedType.LPWStr)] string value);
    
    [LibraryImport("mediainfo")]
    private static partial IntPtr MediaInfoA_Option(IntPtr handle, IntPtr option,  IntPtr value);
    
    [LibraryImport("mediainfo")]
    private static partial IntPtr MediaInfo_State_Get(IntPtr handle);
    
    [LibraryImport("mediainfo")]
    private static partial IntPtr MediaInfo_Count_Get(IntPtr handle, IntPtr streamKind, IntPtr streamNumber);
    
    private readonly IntPtr _handle;
    
    private readonly bool _mustUseAnsi;
    
    public MediaInfo()
    {
        try
        {
            _handle = MediaInfo_New();
        }
        catch
        {
            _handle = 0;
        }

        _mustUseAnsi = !OperatingSystem.IsWindows();
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
    
    private void ReleaseUnmanagedResources()
    {
        if (_handle != 0)
            MediaInfo_Close(_handle);
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
        
        if (!_mustUseAnsi)
            return (int)MediaInfo_Open(_handle, fileName);
        
        var fileNamePtr = Marshal.StringToHGlobalAnsi(fileName);
        var toReturn = (int)MediaInfoA_Open(_handle, fileNamePtr);
        Marshal.FreeHGlobal(fileNamePtr);
        return toReturn;
    }
    
    public string? Inform()
    {
        if (_handle == 0)
            return "Unable to load MediaInfo library";
        
        return _mustUseAnsi ? Marshal.PtrToStringAnsi(MediaInfoA_Inform(_handle, 0))
            : Marshal.PtrToStringUni(MediaInfo_Inform(_handle, 0));
    }
    
    public string? Get(StreamKind streamKind, int streamNumber, string parameter, InfoKind kindOfInfo = InfoKind.Text, InfoKind kindOfSearch = InfoKind.Name)
    {
        if (_handle == 0)
            return "Unable to load MediaInfo library";
        
        if (!_mustUseAnsi)
            return Marshal.PtrToStringUni(MediaInfo_Get(_handle, (IntPtr)streamKind, streamNumber, parameter,
                (IntPtr)kindOfInfo, (IntPtr)kindOfSearch));
        
        var parameterPtr = Marshal.StringToHGlobalAnsi(parameter);
        var toReturn = Marshal.PtrToStringAnsi(MediaInfoA_Get(_handle, (IntPtr)streamKind, streamNumber, parameterPtr,
            (IntPtr)kindOfInfo, (IntPtr)kindOfSearch));
        Marshal.FreeHGlobal(parameterPtr);
        return toReturn;
    }
    
    public string? Get(StreamKind streamKind, int streamNumber, int parameter, InfoKind kindOfInfo = InfoKind.Text)
    {
        if (_handle == 0)
            return "Unable to load MediaInfo library";

        return _mustUseAnsi ?
            Marshal.PtrToStringAnsi(MediaInfoA_GetI(_handle, (IntPtr)streamKind, streamNumber, parameter, (IntPtr)kindOfInfo))
            : Marshal.PtrToStringUni(MediaInfo_GetI(_handle, (IntPtr)streamKind, streamNumber, parameter, (IntPtr)kindOfInfo));
    }
    
    public string? Option(string option, string value = "")
    {
        if (_handle == 0)
            return "Unable to load MediaInfo library";
        
        if (!_mustUseAnsi)
            return Marshal.PtrToStringUni(MediaInfo_Option(_handle, option, value));
        
        var optionPtr = Marshal.StringToHGlobalAnsi(option);
        var valuePtr = Marshal.StringToHGlobalAnsi(value);
        var toReturn = Marshal.PtrToStringAnsi(MediaInfoA_Option(_handle, optionPtr, valuePtr));
        Marshal.FreeHGlobal(optionPtr);
        Marshal.FreeHGlobal(valuePtr);
        return toReturn;
    }
}