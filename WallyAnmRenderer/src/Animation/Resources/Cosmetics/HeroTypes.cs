using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Linq;

namespace WallyAnmRenderer;

public sealed class HeroTypes
{
    private readonly Dictionary<string, HeroType> _heroes = [];

    public HeroTypes(XElement element)
    {
        foreach (XElement hero in element.Elements())
        {
            string name = hero.Attribute("HeroName")!.Value;
            if (name == "Template") continue;
            _heroes[name] = new(hero);
        }
    }

    public bool TryGetHero(string heroName, [MaybeNullWhen(false)] out HeroType hero)
    {
        return _heroes.TryGetValue(heroName, out hero);
    }

    public IEnumerable<string> Heroes => _heroes.Keys;
}