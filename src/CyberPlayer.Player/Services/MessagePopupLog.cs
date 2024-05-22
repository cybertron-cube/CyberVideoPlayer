using System;

namespace CyberPlayer.Player.Services;

[Flags]
public enum MessagePopupLog
{
    None = 0,
    Title = 1,
    Message = 2
}
