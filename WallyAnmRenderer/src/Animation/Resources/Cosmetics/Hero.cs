using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using BrawlhallaAnimLib.Gfx;
using BrawlhallaAnimLib.Reading;

namespace WallyAnmRenderer;

public sealed class Hero
{
    public string Name { get; }
    public string BioName { get; }

    public Hero(string name, string bioName)
    {
        Name = name;
        BioName = bioName;
    }

    public Hero(XElement element)
    {
        Name = element.Attribute("HeroName")?.Value ?? throw new ArgumentException("HeroName missing");
        foreach (XElement child in element.Elements())
        {
            switch (child.Name.LocalName)
            {
                case "BioName":
                    BioName = child.Value;
                    break;
            }
        }

        if (string.IsNullOrEmpty(BioName))
        {
            var heroDisplayName = element.Attribute("HeroDisplayName")?.Value;
            BioName = !string.IsNullOrEmpty(heroDisplayName)
            ? System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(heroDisplayName.ToLower())
            : Name;
        }
    }
}