using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Linq;

namespace WallyAnmRenderer;

public sealed class HeroTypes
{
    private readonly Dictionary<string, Hero> _Heroes = [];

    public HeroTypes(XElement element)
    {
        foreach (XElement hero in element.Elements())
        {
            string name = hero.Attribute("HeroName")?.Value!;
            if (name == "Template") continue;
            _Heroes[name] = new(hero);
        }
    }

    public bool TryGetBioName(string heroName, [MaybeNullWhen(false)] out string bioName)
    {
        Hero? hero;
        if (_Heroes.TryGetValue(heroName, out hero))
        {
            bioName = hero.BioName;
            return true;
        }
        bioName = null;
        return false;
    }

    public IEnumerable<string> Heroes => _Heroes.Keys;
}