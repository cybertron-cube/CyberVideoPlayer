using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace CyberPlayer.Player.Models;

public partial class Activatable<T> : ReactiveObject
{
    public required T Entity { get; init; }
    
    [Reactive]
    public partial bool Activated { get; set; }
}
