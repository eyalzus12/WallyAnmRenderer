using System.Collections.Generic;
using System.Xml.Linq;

namespace WallyAnmRenderer;

public sealed class ColorSchemeTypes
{
    private readonly Dictionary<string, ColorScheme> _colorSchemes = [];

    public ColorSchemeTypes(XElement element)
    {
        foreach (XElement colorScheme in element.Elements())
        {
            string name = colorScheme.Attribute("ColorSchemeName")?.Value!;
            if (name == "Template") continue;
            _colorSchemes[name] = new(colorScheme);
        }
    }
}