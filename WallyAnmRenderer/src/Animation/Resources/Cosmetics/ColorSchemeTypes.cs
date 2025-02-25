using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Linq;

namespace WallyAnmRenderer;

public sealed class ColorSchemeTypes
{
    private readonly Dictionary<string, ColorScheme> _colorSchemes = [];

    public ColorSchemeTypes(XElement element)
    {
        foreach (XElement colorScheme in element.Elements())
        {
            string name = colorScheme.Attribute("ColorSchemeName")!.Value;
            if (name == "Template") continue;
            _colorSchemes[name] = new(colorScheme);
        }
    }

    public bool TryGetColorScheme(string name, [MaybeNullWhen(false)] out ColorScheme colorScheme)
    {
        return _colorSchemes.TryGetValue(name, out colorScheme);
    }

    public IEnumerable<string> ColorSchemes => _colorSchemes.Keys;
}