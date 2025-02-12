using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using BrawlhallaAnimLib.Gfx;
using BrawlhallaAnimLib.Reading;
using nietras.SeparatedValues;

namespace WallyAnmRenderer;

internal readonly record struct ColorExceptionKey(string TargetName, string ColorSwapName, ColorExceptionMode Mode);

public sealed class ColorExceptionType : IColorExceptionType
{
    public string TargetName { get; private set; }
    public string ColorSchemeName { get; private set; }
    public ColorExceptionMode Mode { get; private set; }

    internal ColorExceptionKey Key => new(TargetName, ColorSchemeName, Mode);

    private readonly Dictionary<ColorSchemeSwapEnum, ColorSchemeSwapEnum> _redirects = [];

    public ColorExceptionType(ICsvRow row)
    {
        TargetName = null!;
        ColorSchemeName = null!;
        Mode = ColorExceptionMode.Costume;
        foreach ((string key, string value) in row.ColEntries)
        {
            if (value == "") continue;

            if (key == "TargetName")
            {
                TargetName = value;
            }
            else if (key == "ColorSchemeName")
            {
                ColorSchemeName = value;
            }
            else if (key == "ExceptionMode")
            {
                Mode = value == "Weapon" ? ColorExceptionMode.Weapon : ColorExceptionMode.Costume;
            }
            else if (key.EndsWith("_Swap"))
            {
                string swap = key[..^"_Swap".Length];
                if (!Enum.TryParse(swap, true, out ColorSchemeSwapEnum swapType))
                    throw new ArgumentException($"Invalid swap {swap}");

                if (!Enum.TryParse(value, true, out ColorSchemeSwapEnum target))
                    throw new ArgumentException($"Invalid swap {value}");
                _redirects[swapType] = target;
            }
        }
    }

    public bool TryGetSwapRedirect(ColorSchemeSwapEnum swap, out ColorSchemeSwapEnum newSwap)
    {
        return _redirects.TryGetValue(swap, out newSwap);
    }
}

public sealed class ColorExceptionTypes : IColorExceptionTypes
{
    private readonly Dictionary<ColorExceptionKey, ColorExceptionType> _exceptions = [];

    public ColorExceptionTypes(SepReader reader)
    {
        foreach (SepReader.Row row in reader)
        {
            string key = row["ColorExceptionName"].ToString();
            if (key == "Template") continue;
            SepRowAdapter adapter = new(reader.Header, row, key);
            ColorExceptionType colorException = new(adapter);
            _exceptions[colorException.Key] = colorException;
        }
    }

    public bool TryGetColorException(string targetName, string colorSchemeName, ColorExceptionMode mode, [MaybeNullWhen(false)] out IColorExceptionType exception)
    {
        ColorExceptionKey key = new(targetName, colorSchemeName, mode);
        if (_exceptions.TryGetValue(key, out ColorExceptionType? colorException))
        {
            exception = colorException;
            return true;
        }
        exception = null;
        return false;
    }
}