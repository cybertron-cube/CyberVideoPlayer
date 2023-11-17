using System;
using System.Runtime.InteropServices;

namespace CyberPlayer.Player.Business;

public static class MediaInfoFunctions
{
    public delegate IntPtr MediaInfo_New();

    public delegate void MediaInfo_Delete(IntPtr handle);

    public delegate IntPtr MediaInfo_Open(IntPtr handle, [MarshalAs(UnmanagedType.LPWStr)] string fileName);

    public delegate IntPtr MediaInfoA_Open(IntPtr handle, IntPtr fileName);

    //public delegate IntPtr MediaInfoA_Open2(IntPtr handle, long fileSize, long fileOffset);

    public delegate IntPtr MediaInfo_Open_Buffer_Init(IntPtr handle, long fileSize, long fileOffset);

    public delegate IntPtr MediaInfo_Open_Buffer_Continue(IntPtr handle, IntPtr buffer, IntPtr bufferSize);

    public delegate IntPtr MediaInfoA_Open_Buffer_Continue(IntPtr handle, long fileSize, byte[] buffer, IntPtr bufferSize);

    public delegate long MediaInfo_Open_Buffer_Continue_GoTo_Get(IntPtr handle);

    public delegate long MediaInfoA_Open_Buffer_Continue_GoTo_Get(IntPtr handle);

    public delegate IntPtr MediaInfo_Open_Buffer_Finalize(IntPtr handle);

    public delegate IntPtr MediaInfoA_Open_Buffer_Finalize(IntPtr handle);

    public delegate void MediaInfo_Close(IntPtr handle);

    public delegate IntPtr MediaInfo_Inform(IntPtr handle, IntPtr reserved);

    public delegate IntPtr MediaInfoA_Inform(IntPtr handle, IntPtr reserved);

    public delegate IntPtr MediaInfo_GetI(IntPtr handle, IntPtr streamKind, IntPtr streamNumber, IntPtr parameter, IntPtr kindOfInfo);

    public delegate IntPtr MediaInfoA_GetI(IntPtr handle, IntPtr streamKind, IntPtr streamNumber, IntPtr parameter, IntPtr kindOfInfo);

    public delegate IntPtr MediaInfo_Get(IntPtr handle, IntPtr streamKind, IntPtr streamNumber, [MarshalAs(UnmanagedType.LPWStr)] string parameter, IntPtr kindOfInfo, IntPtr kindOfSearch);

    public delegate IntPtr MediaInfoA_Get(IntPtr handle, IntPtr streamKind, IntPtr streamNumber, IntPtr parameter, IntPtr kindOfInfo, IntPtr kindOfSearch);

    public delegate IntPtr MediaInfo_Option(IntPtr handle, [MarshalAs(UnmanagedType.LPWStr)] string option, [MarshalAs(UnmanagedType.LPWStr)] string value);

    public delegate IntPtr MediaInfoA_Option(IntPtr handle, IntPtr option,  IntPtr value);

    public delegate IntPtr MediaInfo_State_Get(IntPtr handle);

    public delegate IntPtr MediaInfo_Count_Get(IntPtr handle, IntPtr streamKind, IntPtr streamNumber);
}