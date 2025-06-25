using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using BrawlhallaAnimLib.Reading;
using nietras.SeparatedValues;

namespace WallyAnmRenderer;

public readonly record struct AvatarTypeInfo(string AvatarName, uint AvatarID, string DisplayNameKey, uint InventoryOrderID, uint InventorySubOrderID);

public sealed class AvatarTypes : ICsvReader
{
    private readonly Dictionary<string, SepRowAdapter> _rows = [];
    private readonly Dictionary<string, AvatarTypeInfo> _infos = [];
    private readonly Dictionary<string, AvatarTypesGfx> _gfx = [];

    public AvatarTypes(SepReader reader)
    {
        foreach (SepReader.Row row in reader)
        {
            string avatarName = row["AvatarName"].ToString();
            if (avatarName == "Template") continue;
            SepRowAdapter adapter = new(reader.Header, row, avatarName);
            _rows[avatarName] = adapter;

            string displayNameKey = row["DisplayNameKey"].ToString();
            _ = uint.TryParse(row["AvatarID"].ToString(), out uint avatarId);
            _ = uint.TryParse(row["InventoryOrderID"].ToString(), out uint orderId);
            _ = uint.TryParse(row["InventorySubOrderID"].ToString(), out uint subOrderId);

            _infos[avatarName] = new(avatarName, avatarId, displayNameKey, orderId, subOrderId);

            AvatarTypesGfx gfx = new(adapter);
            _gfx[avatarName] = gfx;
        }

        _infos = _infos.OrderBy(x => x.Value.InventoryOrderID)
                        .ThenBy(x => x.Value.InventorySubOrderID)
                        .ThenBy(x => x.Value.AvatarID)
                        .ToDictionary(x => x.Key, x => x.Value);
        Dictionary<string, AvatarTypesGfx> sortedGfx = [];
        foreach (string key in _infos.Keys)
        {
            if (_gfx.TryGetValue(key, out AvatarTypesGfx? gfxValue))
            {
                sortedGfx[key] = gfxValue;
            }
        }
        _gfx = sortedGfx;
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

    public bool TryGetGfx(string name, [MaybeNullWhen(false)] out AvatarTypesGfx avatar)
    {
        return _gfx.TryGetValue(name, out avatar);
    }

    public bool TryGetInfo(string name, [MaybeNullWhen(false)] out AvatarTypeInfo info)
    {
        return _infos.TryGetValue(name, out info);
    }

    public IEnumerable<string> Avatars => _gfx.Keys;
}