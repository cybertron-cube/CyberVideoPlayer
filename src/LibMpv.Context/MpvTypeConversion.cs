using System.Collections.Frozen;
using System.Runtime.InteropServices;
using LibMpv.Client;

namespace LibMpv.Context;

public unsafe partial class MpvContext
{
    private static readonly FrozenDictionary<Type, mpv_format> FormatTypeMapping = new Dictionary<Type, mpv_format>
    {
        { typeof(string), mpv_format.MPV_FORMAT_STRING },
        { typeof(bool), mpv_format.MPV_FORMAT_FLAG },
        { typeof(long), mpv_format.MPV_FORMAT_INT64 },
        { typeof(double), mpv_format.MPV_FORMAT_DOUBLE },
        { typeof(Dictionary<string, object?>), mpv_format.MPV_FORMAT_NODE },
    }.ToFrozenDictionary();
    
    private static object? MpvDataToSharpType(nint data, mpv_format format)
    {
        switch (format)
        {
            case mpv_format.MPV_FORMAT_NONE:
                return null;
            case mpv_format.MPV_FORMAT_STRING:
                return MarshalHelper.PtrToStringUTF8OrEmpty(data);
            case mpv_format.MPV_FORMAT_INT64:
                return Marshal.ReadInt64(data);
            case mpv_format.MPV_FORMAT_FLAG:
            {
                var flag = Marshal.ReadInt32(data);
                return flag == 1;
            }
            case mpv_format.MPV_FORMAT_DOUBLE:
            {
                var doubleBytes = new byte[sizeof(double)];
                Marshal.Copy(data, doubleBytes, 0, sizeof(double));
                return BitConverter.ToDouble(doubleBytes, 0);
            }
            case mpv_format.MPV_FORMAT_NODE:
            {
                var node = MarshalHelper.PtrToStructure<mpv_node>(data);
                return MpvNodeToSharpType(node);
            }
            case mpv_format.MPV_FORMAT_NODE_MAP:
            {
                var nodeList = MarshalHelper.PtrToStructure<mpv_node_list>(data);

                var dict = new Dictionary<string, object?>();
                for (var i = 0; i < nodeList.num; i++)
                {
                    var keyStrPointer = (nint)nodeList.keys[i];
                    var key = (string?)MpvDataToSharpType(keyStrPointer, mpv_format.MPV_FORMAT_STRING);
                    var value = nodeList.values[i];
                    
                    dict[key ?? string.Empty] = MpvNodeToSharpType(value);
                }

                return dict;
            }
            case mpv_format.MPV_FORMAT_OSD_STRING:
            case mpv_format.MPV_FORMAT_NODE_ARRAY:
            case mpv_format.MPV_FORMAT_BYTE_ARRAY:
            default:
                throw new ArgumentOutOfRangeException(nameof(format));
        }
    }

    private static object? MpvNodeToSharpType(mpv_node node)
    {
        return node.format switch
        {
            mpv_format.MPV_FORMAT_NONE => null,
            mpv_format.MPV_FORMAT_FLAG => node.u.flag == 1,
            mpv_format.MPV_FORMAT_INT64 => node.u.int64,
            mpv_format.MPV_FORMAT_DOUBLE => node.u.double_,
            mpv_format.MPV_FORMAT_STRING => MpvDataToSharpType((nint)node.u.@string, node.format),
            mpv_format.MPV_FORMAT_NODE_MAP => MpvDataToSharpType((nint)node.u.list, node.format),
            mpv_format.MPV_FORMAT_BYTE_ARRAY => MpvDataToSharpType((nint)node.u.ba, node.format),
            _ => throw new ArgumentOutOfRangeException(nameof(node))
        };
    }
}
