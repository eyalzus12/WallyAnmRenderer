using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using BrawlhallaAnimLib.Reading;
using BrawlhallaAnimLib.Reading.WeaponSkinTypes;
using nietras.SeparatedValues;

namespace WallyAnmRenderer;

public readonly struct WeaponSkinTypes : ICsvReader
{
    private readonly Dictionary<string, SepRowAdapter> _rows = [];
    public Dictionary<string, WeaponSkinTypesGfx> GfxInfo { get; } = [];

    public WeaponSkinTypes(SepReader reader, CostumeTypes costumeTypes)
    {
        foreach (SepReader.Row row in reader)
        {
            string key = row["WeaponSkinName"].ToString();
            if (key == "Template") continue;
            SepRowAdapter adapter = new(reader.Header, row, key);
            _rows[key] = adapter;

            WeaponSkinTypesGfx info = new(adapter, costumeTypes);
            GfxInfo[key] = info;
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
}
