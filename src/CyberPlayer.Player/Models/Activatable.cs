using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace CyberPlayer.Player.Models;

public class Activatable<T> : ReactiveObject
{
    public required T Entity { get; init; }
    
    [Reactive]
    public bool Activated { get; set; }
}
