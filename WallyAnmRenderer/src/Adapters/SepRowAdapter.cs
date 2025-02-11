using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using BrawlhallaAnimLib.Reading;
using BrawlhallaAnimLib.Reading.CostumeTypes;
using BrawlhallaAnimLib.Reading.WeaponSkinTypes;
using nietras.SeparatedValues;

namespace WallyAnmRenderer;

public readonly struct CostumeTypes : ICsvReader
{
    private readonly SepReader _reader;
    private readonly Dictionary<string, SepRowAdapter> _rows = [];
    public Dictionary<string, CostumeTypesGfxInfo> GfxInfo { get; } = [];

    public CostumeTypes(SepReader reader)
    {
        _reader = reader;

        foreach (SepReader.Row row in _reader)
        {
            string key = row["CostumeName"].ToString();
            if (key == "Template") continue;
            SepRowAdapter adapter = new(_reader.Header, row, key);
            _rows[key] = adapter;

            CostumeTypesGfxInfo info = CostumeTypesCsvReader.GetGfxTypeInfo(adapter);
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

public readonly struct WeaponSkinTypes : ICsvReader
{
    private readonly SepReader _reader;
    private readonly Dictionary<string, SepRowAdapter> _rows = [];
    public Dictionary<string, WeaponSkinTypesGfxInfo> GfxInfo { get; } = [];

    public WeaponSkinTypes(SepReader reader, CostumeTypes costumeTypes)
    {
        _reader = reader;

        foreach (SepReader.Row row in _reader)
        {
            string key = row["WeaponSkinName"].ToString();
            if (key == "Template") continue;
            SepRowAdapter adapter = new(_reader.Header, row, key);
            _rows[key] = adapter;

            WeaponSkinTypesGfxInfo info = WeaponSkinTypesReader.GetGfxTypeInfo(adapter, costumeTypes);
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

public readonly struct SepRowAdapter : ICsvRow
{
    private readonly string _rowKey;
    private readonly List<KeyValuePair<string, string>> _colEntries = [];

    public SepRowAdapter(SepReaderHeader header, SepReader.Row row, string rowKey)
    {
        _rowKey = rowKey;
        foreach (string colName in header.ColNames)
        {
            SepReader.Col col = row[colName];
            _colEntries.Add(new KeyValuePair<string, string>(colName, col.ToString()));
        }
    }

    public string RowKey => _rowKey;
    public IEnumerable<KeyValuePair<string, string>> ColEntries => _colEntries;
}