using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using BrawlhallaAnimLib.Reading;
using nietras.SeparatedValues;

namespace WallyAnmRenderer;

public readonly record struct ItemTypeInfo(string ItemName, string DisplayNameKey);

public sealed class ItemTypes : ICsvReader
{
    private readonly Dictionary<string, SepRowAdapter> _rows = [];
    private readonly Dictionary<string, ItemTypeInfo> _infos = [];
    private readonly Dictionary<string, ItemTypesGfx> _gfx = [];

    public ItemTypes(SepReader reader)
    {
        foreach (SepReader.Row row in reader)
        {
            string itemName = row["ItemName"].ToString();
            if (itemName == "XLTemplate") continue;
            SepRowAdapter adapter = new(reader.Header, row, itemName);
            _rows[itemName] = adapter;

            string displayNameKey = row["DisplayNameKey"].ToString();
            _infos[itemName] = new(itemName, displayNameKey);

            _gfx[itemName] = new(adapter);
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

    public bool TryGetGfx(string name, [MaybeNullWhen(false)] out ItemTypesGfx item)
    {
        return _gfx.TryGetValue(name, out item);
    }

    public bool TryGetInfo(string name, [MaybeNullWhen(false)] out ItemTypeInfo item)
    {
        return _infos.TryGetValue(name, out item);
    }

    public IEnumerable<string> Items => _gfx.Keys;
}