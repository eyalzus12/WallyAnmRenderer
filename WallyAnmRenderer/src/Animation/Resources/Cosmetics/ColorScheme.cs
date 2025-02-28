using System;
using System.Collections.Generic;
using System.Xml.Linq;
using BrawlhallaAnimLib.Gfx;
using BrawlhallaAnimLib.Reading;

namespace WallyAnmRenderer;

public sealed class ColorScheme : IColorSchemeType
{
    public string Name { get; set; }
    public string? DisplayNameKey { get; }
    public uint OrderId { get; } = 0;
    public uint TeamColor { get; } = 0;
    private readonly Dictionary<ColorSchemeSwapEnum, uint> _swaps = [];

    public ColorScheme(string name)
    {
        Name = name;
    }

    public ColorScheme(string name, IReadOnlyDictionary<ColorSchemeSwapEnum, uint> swaps) : this(name)
    {
        _swaps = new(swaps);
    }

    public ColorScheme(ColorScheme other)
    {
        Name = other.Name;
        DisplayNameKey = other.DisplayNameKey;
        _swaps = new(other._swaps);
    }

    public ColorScheme(XElement element)
    {
        Name = element.Attribute("ColorSchemeName")?.Value ?? throw new ArgumentException("ColorSchemeName missing");
        DisplayNameKey = element.Element("DisplayNameKey")?.Value;
        OrderId = uint.TryParse(element.Element("OrderID")?.Value, out uint orderId) ? orderId : 0;
        TeamColor = uint.TryParse(element.Element("TeamColor")?.Value, out uint teamColor) ? teamColor : 0;
        foreach (XElement child in element.Elements())
        {
            string name = child.Name.LocalName;
            if (!name.EndsWith("_Swap")) continue;

            string swap = name[..^"_Swap".Length];
            if (!Enum.TryParse(swap, true, out ColorSchemeSwapEnum swapType))
                throw new ArgumentException($"Invalid swap {swap}");

            string value = child.Value;
            _swaps[swapType] = ParserUtils.ParseHexString(value);
        }
    }

    public uint GetSwap(ColorSchemeSwapEnum swapType)
    {
        return _swaps.GetValueOrDefault(swapType, 0u);
    }

    public uint this[ColorSchemeSwapEnum swapType]
    {
        get => _swaps.GetValueOrDefault(swapType, 0u);
        set
        {
            if (value == 0)
            {
                _swaps.Remove(swapType);
            }
            else
            {
                _swaps[swapType] = value;
            }
        }
    }

    public static ColorScheme DEBUG { get; } = new("DEBUG", new Dictionary<ColorSchemeSwapEnum, uint>()
    {
        [ColorSchemeSwapEnum.HairLt] = 0xFF8080,
        [ColorSchemeSwapEnum.Hair] = 0xFF0000,
        [ColorSchemeSwapEnum.HairDk] = 0x800000,

        [ColorSchemeSwapEnum.Body1VL] = 0xFFE0C0,
        [ColorSchemeSwapEnum.Body1Lt] = 0xFFC080,
        [ColorSchemeSwapEnum.Body1] = 0xFF8000,
        [ColorSchemeSwapEnum.Body1Dk] = 0x804000,
        [ColorSchemeSwapEnum.Body1VD] = 0x402000,
        [ColorSchemeSwapEnum.Body1Acc] = 0xFFC000,

        [ColorSchemeSwapEnum.Body2VL] = 0xFFFFC0,
        [ColorSchemeSwapEnum.Body2Lt] = 0xFFFF80,
        [ColorSchemeSwapEnum.Body2] = 0xFFFF00,
        [ColorSchemeSwapEnum.Body2Dk] = 0x808000,
        [ColorSchemeSwapEnum.Body2VD] = 0x404000,
        [ColorSchemeSwapEnum.Body2Acc] = 0xC0FF00,

        [ColorSchemeSwapEnum.SpecialVL] = 0xC0FFC0,
        [ColorSchemeSwapEnum.SpecialLt] = 0x80FF80,
        [ColorSchemeSwapEnum.Special] = 0x00FF00,
        [ColorSchemeSwapEnum.SpecialDk] = 0x008000,
        [ColorSchemeSwapEnum.SpecialVD] = 0x004000,
        [ColorSchemeSwapEnum.SpecialAcc] = 0x00FFC0,

        // No in-game color scheme defines hand swaps
        /*
        [ColorSchemeSwapEnum.HandsLt] = 0x00FFFF,
        [ColorSchemeSwapEnum.HandsDk] = 0x008080,
        [ColorSchemeSwapEnum.HandsSkinLt] = 0x8000FF,
        [ColorSchemeSwapEnum.HandsSkinDk] = 0x400080,
        */

        [ColorSchemeSwapEnum.ClothVL] = 0xC0C0FF,
        [ColorSchemeSwapEnum.ClothLt] = 0x8080FF,
        [ColorSchemeSwapEnum.Cloth] = 0x0000FF,
        [ColorSchemeSwapEnum.ClothDk] = 0x000080,

        [ColorSchemeSwapEnum.WeaponVL] = 0xFFC0FF,
        [ColorSchemeSwapEnum.WeaponLt] = 0xFF80FF,
        [ColorSchemeSwapEnum.Weapon] = 0xFF00FF,
        [ColorSchemeSwapEnum.WeaponDk] = 0x800080,
        [ColorSchemeSwapEnum.WeaponAcc] = 0xFF0080,
    });
}