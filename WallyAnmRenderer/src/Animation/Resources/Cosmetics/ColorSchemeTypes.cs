using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Linq;
using System.Linq;

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

        _colorSchemes = _colorSchemes.OrderBy(x => x.Value.TeamColor)
                .ThenBy(x => x.Value.TeamColor == 0 ? x.Value.OrderId : 0)
                .ThenBy(x => x.Value.Name)
                .ToDictionary(x => x.Key, x => x.Value);
    }

    public bool TryGetColorScheme(string name, [MaybeNullWhen(false)] out ColorScheme colorScheme)
    {
        return _colorSchemes.TryGetValue(name, out colorScheme);
    }

    public IEnumerable<string> ColorSchemes => _colorSchemes.Keys;
}