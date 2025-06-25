using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Xml.Linq;
using BrawlhallaAnimLib.Reading.PodiumTypes;

namespace WallyAnmRenderer;

public readonly record struct PodiumTypeInfo(string PodiumName, uint PodiumID, string DisplayNameKey, uint DisplayOrderID)
{
    public static PodiumTypeInfo From(XElement element)
    {
        string podiumName = element.Attribute("PodiumName")?.Value ?? throw new ArgumentException("Missing PodiumName");
        uint podiumId = uint.TryParse(element.Element("PodiumId")?.Value, out uint podiumId_) ? podiumId_ : 0;
        string displayNameKey = element.Element("DisplayNameKey")?.Value ?? throw new ArgumentException("Missing DisplayNameKey");
        uint displayOrderId = uint.TryParse(element.Element("DisplayOrderId")?.Value, out uint displayOrderId_) ? displayOrderId_ : 0;
        return new(podiumName, podiumId, displayNameKey, displayOrderId);
    }
}

public sealed class PodiumTypes
{
    private readonly Dictionary<string, PodiumTypeInfo> _infos = [];
    private readonly Dictionary<string, PodiumTypesGfx> _gfx = [];

    public PodiumTypes(XElement element)
    {
        foreach (XElement child in element.Elements())
        {
            string name = child.Attribute("PodiumName")!.Value;
            if (name == "Template") continue;

            PodiumTypeInfo info = PodiumTypeInfo.From(child);
            _infos[info.PodiumName] = info;
            PodiumTypesGfx gfx = new(child);
            _gfx[info.PodiumName] = gfx;
        }

        // sort (TODO: this is bad)
        _infos = _infos.OrderBy(x => x.Value.DisplayOrderID)
                        .ThenBy(x => x.Value.PodiumID)
                        .ToDictionary(x => x.Key, x => x.Value);
        Dictionary<string, PodiumTypesGfx> sortedGfx = [];
        foreach (string key in _infos.Keys)
        {
            if (_gfx.TryGetValue(key, out PodiumTypesGfx? gfxValue))
            {
                sortedGfx[key] = gfxValue;
            }
        }
        _gfx = sortedGfx;
    }

    public bool TryGetGfx(string name, [MaybeNullWhen(false)] out PodiumTypesGfx podium)
    {
        return _gfx.TryGetValue(name, out podium);
    }

    public bool TryGetInfo(string name, [MaybeNullWhen(false)] out PodiumTypeInfo info)
    {
        return _infos.TryGetValue(name, out info);
    }

    public IEnumerable<string> Podiums => _gfx.Keys;
}