using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Linq;
using BrawlhallaAnimLib.Reading;

namespace WallyAnmRenderer;

public readonly record struct CompnaionTypeInfo(string CompanionName, string DisplayNameKey)
{
    public static CompnaionTypeInfo From(XElement element)
    {
        string companionName = element.Attribute("CompanionName")?.Value ?? throw new ArgumentException("Missing CompanionName");
        string displayNameKey = element.Element("DisplayNameKey")?.Value ?? throw new ArgumentException("Missing DisplayNameKey");
        return new(companionName, displayNameKey);
    }
}

public sealed class CompanionTypes
{
    private readonly Dictionary<string, CompnaionTypeInfo> _infos = [];
    private readonly Dictionary<string, CompanionTypesGfx> _gfx = [];

    public CompanionTypes(XElement element)
    {
        foreach (XElement child in element.Elements())
        {
            string name = child.Attribute("CompanionName")!.Value;
            if (name == "Template") continue;

            CompnaionTypeInfo info = CompnaionTypeInfo.From(child);
            _infos[info.CompanionName] = info;
            CompanionTypesGfx gfx = new(child);
            _gfx[info.CompanionName] = gfx;
        }
    }

    public bool TryGetGfx(string name, [MaybeNullWhen(false)] out CompanionTypesGfx spawnBot)
    {
        return _gfx.TryGetValue(name, out spawnBot);
    }

    public bool TryGetInfo(string name, [MaybeNullWhen(false)] out CompnaionTypeInfo info)
    {
        return _infos.TryGetValue(name, out info);
    }

    public IEnumerable<string> Companions => _gfx.Keys;
}