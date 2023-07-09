using System.Reflection;
using System.Xml.Serialization;

namespace CyberPlayer.Player.AppSettings;

public class Settings
{
    [XmlAttribute(AttributeName = "AppVersion")]
    public string Version { get; set; } = Assembly.GetEntryAssembly().GetName().Version.ToString();
    
    [XmlElement]
    public bool UpdaterIncludePreReleases { get; set; } = false;

    [XmlElement]
    public int SeekRefreshRate { get; set; } = 100;

    [XmlElement]
    public int TimeCodeLength { get; set; } = 8;
}