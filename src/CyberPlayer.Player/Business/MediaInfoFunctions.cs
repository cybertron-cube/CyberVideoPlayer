using System;
using System.Runtime.InteropServices;
// ReSharper disable InconsistentNaming

namespace CyberPlayer.Player.Business;

public static class MediaInfoFunctions
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate IntPtr MediaInfo_New();
    
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void MediaInfo_Delete(IntPtr handle);
    
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate IntPtr MediaInfo_Open(IntPtr handle, [MarshalAs(UnmanagedType.LPWStr)] string fileName);
    
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate IntPtr MediaInfoA_Open(IntPtr handle, IntPtr fileName);
    
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate IntPtr MediaInfo_Open_Buffer_Init(IntPtr handle, long fileSize, long fileOffset);
    
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate IntPtr MediaInfo_Open_Buffer_Continue(IntPtr handle, IntPtr buffer, IntPtr bufferSize);
    
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate IntPtr MediaInfoA_Open_Buffer_Continue(IntPtr handle, long fileSize, byte[] buffer, IntPtr bufferSize);
    
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate long MediaInfo_Open_Buffer_Continue_GoTo_Get(IntPtr handle);
    
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate long MediaInfoA_Open_Buffer_Continue_GoTo_Get(IntPtr handle);
    
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate IntPtr MediaInfo_Open_Buffer_Finalize(IntPtr handle);
    
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate IntPtr MediaInfoA_Open_Buffer_Finalize(IntPtr handle);
    
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void MediaInfo_Close(IntPtr handle);
    
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate IntPtr MediaInfo_Inform(IntPtr handle, IntPtr reserved);
    
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate IntPtr MediaInfoA_Inform(IntPtr handle, IntPtr reserved);
    
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate IntPtr MediaInfo_GetI(IntPtr handle, IntPtr streamKind, IntPtr streamNumber, IntPtr parameter, IntPtr kindOfInfo);
    
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate IntPtr MediaInfoA_GetI(IntPtr handle, IntPtr streamKind, IntPtr streamNumber, IntPtr parameter, IntPtr kindOfInfo);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate IntPtr MediaInfo_Get(IntPtr handle, IntPtr streamKind, IntPtr streamNumber, [MarshalAs(UnmanagedType.LPWStr)] string parameter, IntPtr kindOfInfo, IntPtr kindOfSearch);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate IntPtr MediaInfoA_Get(IntPtr handle, IntPtr streamKind, IntPtr streamNumber, IntPtr parameter, IntPtr kindOfInfo, IntPtr kindOfSearch);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate IntPtr MediaInfo_Option(IntPtr handle, [MarshalAs(UnmanagedType.LPWStr)] string option, [MarshalAs(UnmanagedType.LPWStr)] string value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate IntPtr MediaInfoA_Option(IntPtr handle, IntPtr option,  IntPtr value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate IntPtr MediaInfo_State_Get(IntPtr handle);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate IntPtr MediaInfo_Count_Get(IntPtr handle, IntPtr streamKind, IntPtr streamNumber);
}
