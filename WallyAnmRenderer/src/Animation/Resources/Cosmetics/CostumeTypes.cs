using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using BrawlhallaAnimLib.Reading;
using BrawlhallaAnimLib.Reading.CostumeTypes;
using nietras.SeparatedValues;

namespace WallyAnmRenderer;

public readonly record struct CostumeTypeInfo(string CostumeName, string DisplayNameKey, string OwnerHero, uint CostumeIndex);

public readonly struct CostumeTypes : ICsvReader
{
    private readonly Dictionary<string, SepRowAdapter> _rows = [];
    private readonly Dictionary<string, CostumeTypeInfo> _infos = [];
    private readonly Dictionary<string, CostumeTypesGfx> _gfx = [];

    public CostumeTypes(SepReader reader)
    {
        foreach (SepReader.Row row in reader)
        {
            string costumeName = row["CostumeName"].ToString();
            if (costumeName == "Template") continue;
            SepRowAdapter adapter = new(reader.Header, row, costumeName);
            _rows[costumeName] = adapter;

            string displayNameKey = row["DisplayNameKey"].ToString();
            string ownerHero = row["OwnerHero"].ToString();
            uint costumeIndex = uint.Parse(row["CostumeIndex"].ToString());
            _infos[costumeName] = new(costumeName, displayNameKey, ownerHero, costumeIndex);

            CostumeTypesGfx info = new(adapter);
            _gfx[costumeName] = info;
        }
    }

    public bool TryGetCol(string key, [MaybeNullWhen(false)] out ICsvRow row)
    {
        if (_rows.TryGetValue(key, out SepRowAdapter adapter))
        {
            row = adapter;
            return true;
        }
        row = null;
        return false;
    }

    public bool TryGetGfx(string name, [MaybeNullWhen(false)] out CostumeTypesGfx costume)
    {
        return _gfx.TryGetValue(name, out costume);
    }

    public bool TryGetInfo(string name, [MaybeNullWhen(false)] out CostumeTypeInfo info)
    {
        return _infos.TryGetValue(name, out info);
    }

    public IEnumerable<string> Costumes => _gfx.Keys;
}