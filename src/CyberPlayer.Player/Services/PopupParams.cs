using System;
using Avalonia;
using Avalonia.Media;

namespace CyberPlayer.Player.Services;

/// <summary>
/// 
/// </summary>
/// <param name="AnimationDuration">default/null: 0.1 seconds</param>
/// <param name="PopupSize"></param>
/// <param name="UnderlayOpacity"></param>
/// <param name="CloseOnClickAway"></param>
/// <param name="AnimateOpacity"></param>
/// <param name="Brush">default/null: Transparent</param>
/// <param name="Padding">default/null: 0</param>
public record PopupParams(
    TimeSpan? AnimationDuration = null,
    double PopupSize = double.NaN,
    double UnderlayOpacity = 0.5,
    bool CloseOnClickAway = false,
    bool AnimateOpacity = true,
    IBrush? Brush = null,
    Thickness? Padding = null
);