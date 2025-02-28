using System;
using System.Xml.Linq;
using System.Globalization;

namespace WallyAnmRenderer;

public sealed class HeroType
{
    public string Name { get; }
    public string BioName { get; }
    public uint ReleaseOrderId { get; } = 0;

    public HeroType(string name, string bioName)
    {
        Name = name;
        BioName = bioName;
    }

    public HeroType(XElement element)
    {
        Name = element.Attribute("HeroName")?.Value ?? throw new ArgumentException("HeroName missing");
        BioName = element.Element("BioName")?.Value ?? string.Empty;
        ReleaseOrderId = uint.TryParse(element.Element("ReleaseOrderID")?.Value, out uint releaseOrderId) ? releaseOrderId : 0;

        if (string.IsNullOrEmpty(BioName))
        {
            string? heroDisplayName = element.Attribute("HeroDisplayName")?.Value;
            BioName = !string.IsNullOrEmpty(heroDisplayName)
                ? CultureInfo.CurrentCulture.TextInfo.ToTitleCase(heroDisplayName.ToLower())
                : Name;
        }
    }
}