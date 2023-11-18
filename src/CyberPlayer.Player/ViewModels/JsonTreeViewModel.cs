using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Controls;
using CyberPlayer.Player.Models;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace CyberPlayer.Player.ViewModels;

public class JsonTreeViewModel : ViewModelBase
{
    private static readonly char[] ValueStartFlags =
    {
        '{', '[', '"',
        '0', '1', '2', '3', '4', '5', '6', '7', '8', '9'
    };

    private record ParentHeader(string Header, int NodeCount) { public int NodeCount { get; set; } = NodeCount; }
    
    [Reactive]
    public string? Title { get; set; }

    private string? _rawText = string.Empty;

    public string? RawText
    {
        get => _rawText;
        set
        {
            if (value == _rawText) return;
            _rawText = value;
            this.RaisePropertyChanged();

            if (_rawText == null) return;
            Items = ConvertJsonToNodes(_rawText!);
            this.RaisePropertyChanged(nameof(Items));
        }
    }
    
    [Reactive]
    public IEnumerable<Node>? Items { get; set; }
    
    public JsonTreeViewModel()
    {
        if (Design.IsDesignMode)
        {
            Title = "Design Mode";
            var currentDir = AppDomain.CurrentDomain.BaseDirectory;
            while (Path.GetFileName(currentDir) != "src")
            {
                currentDir = Path.GetDirectoryName(currentDir);
            }

            RawText = File.ReadAllText(Path.Combine(currentDir, "Tests", "mediainfojsonoutput.json"));
        }
    }

    public static IEnumerable<Node> ConvertJsonToNodes(string text)
    {
        var returnList = new List<Node>();
        Node? currentParentNode = null;
        var parentListHeaders = new List<ParentHeader>();
        int root = 0;
        
        for (int i = 0; i < text.Length; i++)
        {
            var character = text[i];
            if (character == '"')
            {
                var endIndex = text.IndexOf('"', ++i);
                var colonIndex = text.IndexOf(':', i);
                var nextValIndex = text.IndexOfAny(ValueStartFlags, colonIndex + 1);
                
                if (text[nextValIndex] == '{')
                {
                    var node = new Node(text[i..endIndex]);
                    
                    if (currentParentNode == null)
                    {
                        currentParentNode = node;

                        if (root == 0)
                        {
                            returnList.Add(node);
                        }

                        root++;
                    }
                    else
                    {
                        node.Parent = currentParentNode;
                        currentParentNode.Children.Add(node);
                        currentParentNode = node;
                    }
                    
                    i = nextValIndex;
                }
                else if (text[nextValIndex] == '[')
                {
                    parentListHeaders.Add(new ParentHeader(text[i..endIndex], 0));
                    i = nextValIndex;
                }
                else if (text[nextValIndex] == '"')
                {
                    var valueEndIndex = text.IndexOf('"', nextValIndex + 1);
                    var node = new Node(text[i..endIndex], text[(nextValIndex + 1)..valueEndIndex]);
                    node.Parent = currentParentNode;
                    
                    currentParentNode.Children.Add(node);

                    i = valueEndIndex;
                }
                else //integer
                {
                    var valueEndIndex = text.IndexOfAny(new []{ ',', '}' }, nextValIndex + 1);
                    var node = new Node(text[i..endIndex], text[nextValIndex..valueEndIndex]);
                    node.Parent = currentParentNode;
                    
                    currentParentNode.Children.Add(node);

                    i = valueEndIndex - 1;
                }
            }
            else if (character == ']')
            {
                parentListHeaders.RemoveAt(parentListHeaders.Count - 1);
            }
            else if (character == '{' && parentListHeaders.Count != 0)
            {
                var node = new Node(parentListHeaders[^1].Header + ++parentListHeaders[^1].NodeCount);
                if (currentParentNode == null)
                {
                    returnList.Add(node);
                }
                else
                {
                    currentParentNode.Children.Add(node);
                    node.Parent = currentParentNode;
                }
                currentParentNode = node;
            }
            else if (character == '}')
            {
                currentParentNode = currentParentNode?.Parent;
                if (parentListHeaders.Count == 0)
                    root--;
            }
        }

        return returnList;
    }
}