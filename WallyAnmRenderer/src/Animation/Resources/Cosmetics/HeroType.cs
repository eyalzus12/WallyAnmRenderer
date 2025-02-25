using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Globalization;
using BrawlhallaAnimLib.Gfx;
using BrawlhallaAnimLib.Reading;

namespace WallyAnmRenderer;

public sealed class HeroType
{
    public string Name { get; }
    public string BioName { get; }

    public HeroType(string name, string bioName)
    {
        Name = name;
        BioName = bioName;
    }

    public HeroType(XElement element)
    {
        Name = element.Attribute("HeroName")?.Value ?? throw new ArgumentException("HeroName missing");
        BioName = element.Element("BioName")?.Value ?? string.Empty;

        if (string.IsNullOrEmpty(BioName))
        {
            var heroDisplayName = element.Attribute("HeroDisplayName")?.Value;
            BioName = !string.IsNullOrEmpty(heroDisplayName)
            ? CultureInfo.CurrentCulture.TextInfo.ToTitleCase(heroDisplayName.ToLower())
            : Name;
        }
    }
}