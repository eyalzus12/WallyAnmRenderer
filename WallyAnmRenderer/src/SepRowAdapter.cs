using System.Collections.Generic;
using BrawlhallaAnimLib.Reading;
using nietras.SeparatedValues;

namespace WallyAnmRenderer;

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