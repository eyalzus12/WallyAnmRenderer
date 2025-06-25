using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using BrawlhallaAnimLib.Reading;
using nietras.SeparatedValues;

namespace WallyAnmRenderer;

public readonly record struct WeaponSkinTypeInfo(string WeaponSkinName, string DisplayNameKey);

public sealed class WeaponSkinTypes : ICsvReader
{
    private readonly Dictionary<string, SepRowAdapter> _rows = [];
    private readonly Dictionary<string, WeaponSkinTypeInfo> _infos = [];
    private readonly Dictionary<string, WeaponSkinTypesGfx> _gfx = [];

    public WeaponSkinTypes(SepReader reader, CostumeTypes costumeTypes)
    {
        foreach (SepReader.Row row in reader)
        {
            string weaponSkinName = row["WeaponSkinName"].ToString();
            if (weaponSkinName == "Template") continue;
            SepRowAdapter adapter = new(reader.Header, row, weaponSkinName);
            _rows[weaponSkinName] = adapter;

            string displayNameKey = row["DisplayNameKey"].ToString();
            _infos[weaponSkinName] = new(weaponSkinName, displayNameKey);

            WeaponSkinTypesGfx info = new(adapter, costumeTypes);
            _gfx[weaponSkinName] = info;
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

    public bool TryGetGfx(string name, [MaybeNullWhen(false)] out WeaponSkinTypesGfx weaponSkin)
    {
        return _gfx.TryGetValue(name, out weaponSkin);
    }

    public bool TryGetInfo(string name, [MaybeNullWhen(false)] out WeaponSkinTypeInfo info)
    {
        return _infos.TryGetValue(name, out info);
    }

    public IEnumerable<string> WeaponSkins => _gfx.Keys;
}
