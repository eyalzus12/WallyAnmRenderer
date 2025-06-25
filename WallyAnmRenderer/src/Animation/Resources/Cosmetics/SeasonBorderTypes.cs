using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Linq;
using BrawlhallaAnimLib.Reading;

namespace WallyAnmRenderer;

public readonly record struct SeasonBorderTypeInfo(string SeasonBorderName, string DisplayNameKey)
{
    public static SeasonBorderTypeInfo From(XElement element)
    {
        string seasonBorderName = element.Attribute("SeasonBorderName")?.Value ?? throw new ArgumentException("Missing SeasonBorderName");
        string displayNameKey = element.Element("DisplayNameKey")?.Value ?? throw new ArgumentException("Missing DisplayNameKey");
        return new(seasonBorderName, displayNameKey);
    }
}

public sealed class SeasonBorderTypes
{
    private readonly Dictionary<string, SeasonBorderTypeInfo> _infos = [];
    private readonly Dictionary<string, SeasonBorderTypesGfx> _gfx = [];

    public SeasonBorderTypes(XElement element)
    {
        foreach (XElement child in element.Elements())
        {
            string name = child.Attribute("SeasonBorderName")!.Value;
            if (name == "Template") continue;

            SeasonBorderTypeInfo info = SeasonBorderTypeInfo.From(child);
            _infos[info.SeasonBorderName] = info;
            SeasonBorderTypesGfx gfx = new(child);
            _gfx[info.SeasonBorderName] = gfx;
        }
    }

    public bool TryGetGfx(string name, [MaybeNullWhen(false)] out SeasonBorderTypesGfx border)
    {
        return _gfx.TryGetValue(name, out border);
    }

    public bool TryGetInfo(string name, [MaybeNullWhen(false)] out SeasonBorderTypeInfo info)
    {
        return _infos.TryGetValue(name, out info);
    }

    public IEnumerable<string> Borders => _gfx.Keys;
}