using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using BrawlhallaAnimLib.Reading;
using BrawlhallaAnimLib.Reading.CostumeTypes;
using nietras.SeparatedValues;

namespace WallyAnmRenderer;

public readonly struct CostumeTypes : ICsvReader
{
    private readonly Dictionary<string, SepRowAdapter> _rows = [];
    private readonly Dictionary<string, CostumeTypesGfx> _gfx = [];

    public CostumeTypes(SepReader reader)
    {
        foreach (SepReader.Row row in reader)
        {
            string key = row["CostumeName"].ToString();
            if (key == "Template") continue;
            SepRowAdapter adapter = new(reader.Header, row, key);
            _rows[key] = adapter;

            CostumeTypesGfx info = new(adapter);
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

    public bool TryGetGfx(string name, [MaybeNullWhen(false)] out CostumeTypesGfx costume)
    {
        return _gfx.TryGetValue(name, out costume);
    }

    public IEnumerable<string> Costumes => _gfx.Keys;
}