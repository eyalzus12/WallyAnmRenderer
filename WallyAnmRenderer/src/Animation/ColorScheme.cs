using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml.Linq;
using BrawlhallaAnimLib.Gfx;
using BrawlhallaAnimLib.Reading;

namespace WallyAnmRenderer;

public sealed class ColorScheme : IColorSchemeType
{
    public string Name { get; }
    private readonly Dictionary<ColorSchemeSwapEnum, uint> _swaps = [];

    public ColorScheme(XElement element)
    {
        Name = element.Attribute("ColorSchemeName")?.Value ?? throw new ArgumentException("ColorSchemeName missing");
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
}