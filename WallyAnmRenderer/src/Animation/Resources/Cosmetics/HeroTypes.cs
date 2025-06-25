using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Xml.Linq;

namespace WallyAnmRenderer;

public readonly record struct HeroTypeInfo(string Name, string BioName, uint ReleaseOrderId)
{
    public static HeroTypeInfo From(XElement element)
    {
        string name = element.Attribute("HeroName")?.Value ?? throw new ArgumentException("HeroName missing");
        string bioName = element.Element("BioName")?.Value ?? string.Empty;
        uint releaseOrderId = uint.TryParse(element.Element("ReleaseOrderID")?.Value, out uint releaseOrderId_) ? releaseOrderId_ : 0;

        if (string.IsNullOrEmpty(bioName))
        {
            string? heroDisplayName = element.Attribute("HeroDisplayName")?.Value;
            bioName = !string.IsNullOrEmpty(heroDisplayName)
                ? CultureInfo.CurrentCulture.TextInfo.ToTitleCase(heroDisplayName.ToLower())
                : name;
        }

        return new(name, bioName, releaseOrderId);
    }
}

public sealed class HeroTypes
{
    private readonly Dictionary<string, HeroTypeInfo> _heroes = [];

    public HeroTypes(XElement element)
    {
        foreach (XElement hero in element.Elements())
        {
            string name = hero.Attribute("HeroName")!.Value;
            if (name == "Template") continue;
            _heroes[name] = HeroTypeInfo.From(hero);
        }
    }

    public bool TryGetHero(string heroName, [MaybeNullWhen(false)] out HeroTypeInfo hero)
    {
        return _heroes.TryGetValue(heroName, out hero);
    }

    public IEnumerable<string> Heroes => _heroes.Keys;
}