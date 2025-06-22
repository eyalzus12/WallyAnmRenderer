using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Linq;
using BrawlhallaAnimLib.Reading.SpawnBotTypes;

namespace WallyAnmRenderer;

public readonly record struct SpawnBotTypeInfo(string SpawnBotName, string DisplayNameKey)
{
    public static SpawnBotTypeInfo From(XElement element)
    {
        string spawnBotName = element.Attribute("SpawnBotName")?.Value ?? throw new ArgumentException("Missing SpawnBotName");
        string displayNameKey = element.Element("DisplayNameKey")?.Value ?? throw new ArgumentException("Missing DisplayNameKey");
        return new(spawnBotName, displayNameKey);
    }
}

public sealed class SpawnBotTypes
{
    private readonly Dictionary<string, SpawnBotTypeInfo> _infos = [];
    private readonly Dictionary<string, SpawnBotTypesGfx> _gfx = [];

    public SpawnBotTypes(XElement element)
    {
        foreach (XElement child in element.Elements())
        {
            string name = child.Attribute("SpawnBotName")!.Value;
            if (name == "Template") continue;

            SpawnBotTypeInfo info = SpawnBotTypeInfo.From(child);
            _infos[info.SpawnBotName] = info;
            SpawnBotTypesGfx gfx = new(child);
            _gfx[info.SpawnBotName] = gfx;
        }
    }

    public bool TryGetGfx(string name, [MaybeNullWhen(false)] out SpawnBotTypesGfx spawnBot)
    {
        return _gfx.TryGetValue(name, out spawnBot);
    }

    public bool TryGetInfo(string name, [MaybeNullWhen(false)] out SpawnBotTypeInfo info)
    {
        return _infos.TryGetValue(name, out info);
    }

    public IEnumerable<string> SpawnBots => _gfx.Keys;
}