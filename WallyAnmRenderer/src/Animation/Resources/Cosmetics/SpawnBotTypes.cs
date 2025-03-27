using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Linq;
using BrawlhallaAnimLib.Reading;

namespace WallyAnmRenderer;

public sealed class SpawnBotTypes
{
    private readonly Dictionary<string, SpawnBotType> _infos = [];
    private readonly Dictionary<string, SpawnBotTypesGfx> _gfx = [];

    public SpawnBotTypes(XElement element)
    {
        foreach (XElement child in element.Elements())
        {
            string name = child.Attribute("SpawnBotName")!.Value;
            if (name == "Template") continue;

            SpawnBotType info = SpawnBotType.From(child);
            _infos[info.SpawnBotName] = info;
            SpawnBotTypesGfx gfx = new(child);
            _gfx[info.SpawnBotName] = gfx;
        }
    }

    public bool TryGetGfx(string name, [MaybeNullWhen(false)] out SpawnBotTypesGfx spawnBot)
    {
        return _gfx.TryGetValue(name, out spawnBot);
    }

    public bool TryGetInfo(string name, [MaybeNullWhen(false)] out SpawnBotType info)
    {
        return _infos.TryGetValue(name, out info);
    }

    public IEnumerable<string> SpawnBots => _gfx.Keys;
}