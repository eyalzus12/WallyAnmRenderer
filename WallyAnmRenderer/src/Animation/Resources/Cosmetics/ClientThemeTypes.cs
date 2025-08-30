using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Xml.Linq;
using BrawlhallaAnimLib.Reading;

namespace WallyAnmRenderer;

public readonly record struct ClientThemeTypesInfo(string ClientThemeName)
{
    public static ClientThemeTypesInfo From(XElement element)
    {
        string clientThemeName = element.Attribute("ClientThemeName")?.Value ?? throw new ArgumentException("Missing ClientThemeName");
        return new(clientThemeName);
    }
}

public sealed class ClientThemeTypes
{
    private readonly Dictionary<string, ClientThemeTypesInfo> _infos = [];
    private readonly Dictionary<string, ClientThemeTypesGfx> _gfx = [];

    public ClientThemeTypes(XElement element)
    {
        foreach (XElement child in element.Elements())
        {
            string name = child.Attribute("ClientThemeName")!.Value;
            if (name == "Template") continue;

            // ignore those that have no custom art, because they just clutter
            if (child.Element("AnimCustomArt") is null)
                continue;

            ClientThemeTypesInfo info = ClientThemeTypesInfo.From(child);
            _infos[info.ClientThemeName] = info;
            ClientThemeTypesGfx gfx = new(child);
            _gfx[info.ClientThemeName] = gfx;
        }

        // sort (TODO: this is bad)
        _infos = _infos.OrderBy(x => x.Value.ClientThemeName)
                        .ToDictionary(x => x.Key, x => x.Value);
        Dictionary<string, ClientThemeTypesGfx> sortedGfx = [];
        foreach (string key in _infos.Keys)
        {
            if (_gfx.TryGetValue(key, out ClientThemeTypesGfx? gfxValue))
            {
                sortedGfx[key] = gfxValue;
            }
        }
        _gfx = sortedGfx;
    }

    public bool TryGetGfx(string name, [MaybeNullWhen(false)] out ClientThemeTypesGfx theme)
    {
        return _gfx.TryGetValue(name, out theme);
    }

    public bool TryGetInfo(string name, [MaybeNullWhen(false)] out ClientThemeTypesInfo info)
    {
        return _infos.TryGetValue(name, out info);
    }

    public IEnumerable<string> Themes => _gfx.Keys;
}