using System.Collections.Generic;

namespace CyberPlayer.Player.Models;

public class Node
{
    public Node() { }
        
    public Node(string header)
    {
        Header = header;
    }

    public Node(string key, string value)
    {
        Key = key;
        Value = value;
    }
    public Node? Parent { get; set; }
    public List<Node> Children { get; set; } = new();
    public string? Header { get; set; }
    public string? Key { get; set; }
    public string? Value { get; set; }
}