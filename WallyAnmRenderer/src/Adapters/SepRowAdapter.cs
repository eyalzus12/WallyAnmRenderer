using System.Collections.Generic;
using BrawlhallaAnimLib.Reading;
using nietras.SeparatedValues;

namespace WallyAnmRenderer;

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