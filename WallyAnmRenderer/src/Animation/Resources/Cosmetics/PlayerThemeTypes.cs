using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Linq;
using BrawlhallaAnimLib.Reading;

namespace WallyAnmRenderer;

public readonly record struct PlayerThemeTypeInfo(string PlayerThemeName, string DisplayNameKey)
{
    public static PlayerThemeTypeInfo From(XElement element)
    {
        string playerThemeName = element.Attribute("PlayerThemeName")?.Value ?? throw new ArgumentException("Missing PlayerThemeName");
        string displayNameKey = element.Element("DisplayNameKey")?.Value ?? throw new ArgumentException("Missing DisplayNameKey");
        return new(playerThemeName, displayNameKey);
    }
}

public sealed class PlayerThemeTypes
{
    private readonly Dictionary<string, PlayerThemeTypeInfo> _infos = [];
    private readonly Dictionary<string, PlayerThemeTypesGfx> _gfx = [];

    public PlayerThemeTypes(XElement element)
    {
        foreach (XElement child in element.Elements())
        {
            string name = child.Attribute("PlayerThemeName")!.Value;
            if (name == "Template") continue;

            PlayerThemeTypeInfo info = PlayerThemeTypeInfo.From(child);
            _infos[info.PlayerThemeName] = info;
            PlayerThemeTypesGfx gfx = new(child);
            _gfx[info.PlayerThemeName] = gfx;
        }
    }

    public bool TryGetGfx(string name, [MaybeNullWhen(false)] out PlayerThemeTypesGfx uiTheme)
    {
        return _gfx.TryGetValue(name, out uiTheme);
    }

    public bool TryGetInfo(string name, [MaybeNullWhen(false)] out PlayerThemeTypeInfo info)
    {
        return _infos.TryGetValue(name, out info);
    }

    public IEnumerable<string> UIThemes => _gfx.Keys;
}