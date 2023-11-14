using System.Diagnostics;

namespace CyberPlayer.Player.Business;

public class CustomProcess : Process
{
    public bool HasStarted { get; set; }
    
    public new bool Start()
    {
        var result = base.Start();
        HasStarted = true;
        return result;
    }
}