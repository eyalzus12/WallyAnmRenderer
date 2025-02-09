using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using BrawlhallaAnimLib.Reading;
using nietras.SeparatedValues;

namespace WallyAnmRenderer;

public readonly struct SepReaderAdapter_CostumeTypes(SepReader reader) : ICsvReader
{
    public bool TryGetCol(string key, [MaybeNullWhen(false)] out ICsvRow outRow)
    {
        foreach (SepReader.Row row in reader)
        {
            if (row["CostumeName"].ToString() == key)
            {
                outRow = new SepRowAdapter(reader.Header, row);
                return true;
            }
        }
        outRow = null;
        return false;
    }
}

public readonly struct SepReaderAdapter_WeaponSkinTypes(SepReader reader) : ICsvReader
{
    public bool TryGetCol(string key, [MaybeNullWhen(false)] out ICsvRow outRow)
    {
        foreach (SepReader.Row row in reader)
        {
            if (row["WeaponSkinName"].ToString() == key)
            {
                outRow = new SepRowAdapter(reader.Header, row);
                return true;
            }
        }
        outRow = null;
        return false;
    }
}

public readonly struct SepRowAdapter : ICsvRow
{
    private readonly List<KeyValuePair<string, string>> _colEntries = [];

    public SepRowAdapter(SepReaderHeader header, SepReader.Row row)
    {
        foreach (string colName in header.ColNames)
        {
            SepReader.Col col = row[colName];
            _colEntries.Add(new KeyValuePair<string, string>(colName, col.ToString()));
        }
    }

    public IEnumerable<KeyValuePair<string, string>> ColEntries => _colEntries;
}