using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Xml.Linq;
using BrawlhallaAnimLib.Reading;

namespace WallyAnmRenderer;

public readonly record struct EndMatchVoicelineTypesInfo(string EndMatchVoicelineName, string WWiseSoundName, string Category)
{
    public static EndMatchVoicelineTypesInfo From(XElement element)
    {
        string endMatchVoicelineName = element.Attribute("EndMatchVoicelineName")?.Value ?? throw new ArgumentException("Missing EndMatchVoicelineName");
        string wwiseSoundName = element.Element("WWiseSoundName")?.Value ?? throw new ArgumentException("Missing WWiseSoundName");
        string category = element.Element("Category")?.Value ?? throw new ArgumentException("Missing Category");
        return new(endMatchVoicelineName, wwiseSoundName, category);
    }
}

public sealed class EndMatchVoicelineTypes
{
    private readonly Dictionary<string, EndMatchVoicelineTypesInfo> _infos = [];
    private readonly Dictionary<string, EndMatchVoicelineTypesGfx> _gfx = [];

    public EndMatchVoicelineTypes(XElement element)
    {
        foreach (XElement child in element.Elements())
        {
            string name = child.Attribute("EndMatchVoicelineName")!.Value;
            if (name == "Template") continue;

            EndMatchVoicelineTypesInfo info = EndMatchVoicelineTypesInfo.From(child);
            _infos[info.EndMatchVoicelineName] = info;
            EndMatchVoicelineTypesGfx gfx = new(child);
            _gfx[info.EndMatchVoicelineName] = gfx;
        }

        // sort (TODO: this is bad)
        _infos = _infos.OrderBy(x => x.Value.Category)
                        .ThenBy(x => x.Value.EndMatchVoicelineName)
                        .ToDictionary(x => x.Key, x => x.Value);
        Dictionary<string, EndMatchVoicelineTypesGfx> sortedGfx = [];
        foreach (string key in _infos.Keys)
        {
            if (_gfx.TryGetValue(key, out EndMatchVoicelineTypesGfx? gfxValue))
            {
                sortedGfx[key] = gfxValue;
            }
        }
        _gfx = sortedGfx;
    }

    public bool TryGetGfx(string name, [MaybeNullWhen(false)] out EndMatchVoicelineTypesGfx voiceline)
    {
        return _gfx.TryGetValue(name, out voiceline);
    }

    public bool TryGetInfo(string name, [MaybeNullWhen(false)] out EndMatchVoicelineTypesInfo info)
    {
        return _infos.TryGetValue(name, out info);
    }

    public IEnumerable<string> Voicelines => _gfx.Keys;
}