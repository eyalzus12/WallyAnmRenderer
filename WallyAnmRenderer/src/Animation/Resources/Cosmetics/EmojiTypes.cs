using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Xml.Linq;
using BrawlhallaAnimLib.Reading;

namespace WallyAnmRenderer;

public readonly record struct EmojiTypeInfo(string EmojiName, string DisplayNameKey, string Category)
{
    public static EmojiTypeInfo From(XElement element)
    {
        string emojiName = element.Attribute("EmojiName")?.Value ?? throw new ArgumentException("Missing EmojiName");
        string displayNameKey = element.Element("DisplayNameKey")?.Value ?? throw new ArgumentException("Missing DisplayNameKey");
        string category = element.Element("Category")?.Value ?? throw new ArgumentException("Missing Category");
        return new(emojiName, displayNameKey, category);
    }
}

public sealed class EmojiTypes
{
    private readonly Dictionary<string, EmojiTypeInfo> _infos = [];
    private readonly Dictionary<string, EmojiTypesGfx> _gfx = [];

    public EmojiTypes(XElement element)
    {
        foreach (XElement child in element.Elements())
        {
            string name = child.Attribute("EmojiName")!.Value;
            if (name == "Template") continue;

            EmojiTypeInfo info = EmojiTypeInfo.From(child);
            _infos[info.EmojiName] = info;
            EmojiTypesGfx gfx = new(child);
            _gfx[info.EmojiName] = gfx;
        }

        // sort (TODO: this is bad)
        _infos = _infos.OrderBy(x => x.Value.Category)
                        .ThenBy(x => x.Value.EmojiName)
                        .ToDictionary(x => x.Key, x => x.Value);
        Dictionary<string, EmojiTypesGfx> sortedGfx = [];
        foreach (string key in _infos.Keys)
        {
            if (_gfx.TryGetValue(key, out EmojiTypesGfx? gfxValue))
            {
                sortedGfx[key] = gfxValue;
            }
        }
        _gfx = sortedGfx;
    }

    public bool TryGetGfx(string name, [MaybeNullWhen(false)] out EmojiTypesGfx emoji)
    {
        return _gfx.TryGetValue(name, out emoji);
    }

    public bool TryGetInfo(string name, [MaybeNullWhen(false)] out EmojiTypeInfo info)
    {
        return _infos.TryGetValue(name, out info);
    }

    public IEnumerable<string> Emojis => _gfx.Keys;
}