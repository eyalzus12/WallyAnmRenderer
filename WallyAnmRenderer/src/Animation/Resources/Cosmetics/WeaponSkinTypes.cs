using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using BrawlhallaAnimLib.Reading;
using BrawlhallaAnimLib.Reading.WeaponSkinTypes;
using nietras.SeparatedValues;

namespace WallyAnmRenderer;

public readonly struct WeaponSkinTypes : ICsvReader
{
    private readonly Dictionary<string, SepRowAdapter> _rows = [];
    private readonly Dictionary<string, WeaponSkinTypesGfx> _gfx = [];

    public WeaponSkinTypes(SepReader reader, CostumeTypes costumeTypes)
    {
        foreach (SepReader.Row row in reader)
        {
            string key = row["WeaponSkinName"].ToString();
            if (key == "Template") continue;
            SepRowAdapter adapter = new(reader.Header, row, key);
            _rows[key] = adapter;

            WeaponSkinTypesGfx info = new(adapter, costumeTypes);
            _gfx[key] = info;
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

    public bool TryGetGfx(string name, [MaybeNullWhen(false)] out WeaponSkinTypesGfx costume)
    {
        return _gfx.TryGetValue(name, out costume);
    }

    public IEnumerable<string> WeaponSkins => _gfx.Keys;
}
