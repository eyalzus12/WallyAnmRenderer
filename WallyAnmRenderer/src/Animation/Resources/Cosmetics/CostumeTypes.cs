using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using BrawlhallaAnimLib.Reading;
using BrawlhallaAnimLib.Reading.CostumeTypes;
using nietras.SeparatedValues;

namespace WallyAnmRenderer;

public readonly struct CostumeTypes : ICsvReader
{
    private readonly Dictionary<string, SepRowAdapter> _rows = [];
    public Dictionary<string, CostumeTypesGfx> GfxInfo { get; } = [];

    public CostumeTypes(SepReader reader)
    {
        foreach (SepReader.Row row in reader)
        {
            string key = row["CostumeName"].ToString();
            if (key == "Template") continue;
            SepRowAdapter adapter = new(reader.Header, row, key);
            _rows[key] = adapter;

            CostumeTypesGfx info = new(adapter);
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