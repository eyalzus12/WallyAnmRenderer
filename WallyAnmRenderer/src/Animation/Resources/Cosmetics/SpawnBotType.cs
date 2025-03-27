using System;
using System.Xml.Linq;

namespace WallyAnmRenderer;

public sealed class SpawnBotType
{
    public string SpawnBotName { get; }
    public string DisplayNameKey { get; }

    public SpawnBotType(string name, string displayName)
    {
        SpawnBotName = name;
        DisplayNameKey = displayName;
    }

    public static SpawnBotType From(XElement element)
    {
        string spawnBotName = element.Attribute("SpawnBotName")?.Value ?? throw new ArgumentException("Missing SpawnBotName");
        string displayNameKey = element.Element("DisplayNameKey")?.Value ?? throw new ArgumentException("Missing DisplayNameKey");
        return new(spawnBotName, displayNameKey);
    }
}