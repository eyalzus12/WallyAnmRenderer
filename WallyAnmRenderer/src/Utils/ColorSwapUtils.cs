using System;
using System.Collections.Generic;
using SwfLib.Data;
using SwfLib.Gradients;
using SwfLib.Shapes.FillStyles;
using SwfLib.Shapes.Records;
using SwfLib.Tags.ShapeTags;
using SwiffCheese.Wrappers;

namespace WallyAnmRenderer;

public static class ColorSwapUtils
{
    public static void ApplyColorSwaps(ShapeBaseTag shape, Dictionary<uint, uint> colorSwapDict)
    {
        if (colorSwapDict.Count == 0) return;

        if (shape is DefineShapeTag defineShape)
        {
            foreach (FillStyleRGB fillStyle in defineShape.FillStyles)
            {
                ApplyColorSwapFillStyleRGB(fillStyle, colorSwapDict);
            }

            foreach (IShapeRecordRGB shapeRecord in defineShape.ShapeRecords)
            {
                if (shapeRecord is StyleChangeShapeRecordRGB styleChangeShapeRecord)
                {
                    foreach (FillStyleRGB fillStyle in styleChangeShapeRecord.FillStyles)
                    {
                        ApplyColorSwapFillStyleRGB(fillStyle, colorSwapDict);
                    }
                }
            }
        }
        else if (shape is DefineShape2Tag defineShape2)
        {
            foreach (FillStyleRGB fillStyle in defineShape2.FillStyles)
            {
                ApplyColorSwapFillStyleRGB(fillStyle, colorSwapDict);
            }

            foreach (IShapeRecordRGB shapeRecord in defineShape2.ShapeRecords)
            {
                if (shapeRecord is StyleChangeShapeRecordRGB styleChangeShapeRecord)
                {
                    foreach (FillStyleRGB fillStyle in styleChangeShapeRecord.FillStyles)
                    {
                        ApplyColorSwapFillStyleRGB(fillStyle, colorSwapDict);
                    }
                }
            }
        }
        else if (shape is DefineShape3Tag defineShape3)
        {
            foreach (FillStyleRGBA fillStyle in defineShape3.FillStyles)
            {
                ApplyColorSwapFillStyleRGBA(fillStyle, colorSwapDict);
            }

            foreach (IShapeRecordRGBA shapeRecord in defineShape3.ShapeRecords)
            {
                if (shapeRecord is StyleChangeShapeRecordRGBA styleChangeShapeRecord)
                {
                    foreach (FillStyleRGBA fillStyle in styleChangeShapeRecord.FillStyles)
                    {
                        ApplyColorSwapFillStyleRGBA(fillStyle, colorSwapDict);
                    }
                }
            }
        }
        else if (shape is DefineShape4Tag defineShape4)
        {
            foreach (FillStyleRGBA fillStyle in defineShape4.FillStyles)
            {
                ApplyColorSwapFillStyleRGBA(fillStyle, colorSwapDict);
            }

            foreach (IShapeRecordEx shapeRecord in defineShape4.ShapeRecords)
            {
                if (shapeRecord is StyleChangeShapeRecordEx styleChangeShapeRecord)
                {
                    foreach (FillStyleRGBA fillStyle in styleChangeShapeRecord.FillStyles)
                    {
                        ApplyColorSwapFillStyleRGBA(fillStyle, colorSwapDict);
                    }
                }
            }
        }
    }

    private static void ApplyColorSwapFillStyleRGB(FillStyleRGB fillStyle, Dictionary<uint, uint> colorSwapDict)
    {
        if (fillStyle.Type == FillStyleType.SolidColor)
        {
            SolidFillStyleRGB solidFill = (SolidFillStyleRGB)fillStyle;
            solidFill.Color = SwapSwfRGB(solidFill.Color, colorSwapDict);
        }
        else if (fillStyle.Type == FillStyleType.LinearGradient)
        {
            LinearGradientFillStyleRGB linearGradientFill = (LinearGradientFillStyleRGB)fillStyle;
            ApplyColorSwapGradientRGB(linearGradientFill.Gradient, colorSwapDict);
        }
        else if (fillStyle.Type == FillStyleType.RadialGradient)
        {
            RadialGradientFillStyleRGB radialGradientFill = (RadialGradientFillStyleRGB)fillStyle;
            ApplyColorSwapGradientRGB(radialGradientFill.Gradient, colorSwapDict);
        }
        else if (fillStyle.Type == FillStyleType.FocalGradient)
        {
            FocalGradientFillStyleRGB focalGradientFill = (FocalGradientFillStyleRGB)fillStyle;
            ApplyColorSwapGradientRGB(focalGradientFill.Gradient, colorSwapDict);
        }
    }

    private static void ApplyColorSwapFillStyleRGBA(FillStyleRGBA fillStyle, Dictionary<uint, uint> colorSwapDict)
    {
        if (fillStyle.Type == FillStyleType.SolidColor)
        {
            SolidFillStyleRGBA solidFill = (SolidFillStyleRGBA)fillStyle;
            solidFill.Color = SwapSwfRGBA(solidFill.Color, colorSwapDict);
        }
        else if (fillStyle.Type == FillStyleType.LinearGradient)
        {
            LinearGradientFillStyleRGBA linearGradientFill = (LinearGradientFillStyleRGBA)fillStyle;
            ApplyColorSwapGradientRGBA(linearGradientFill.Gradient, colorSwapDict);
        }
        else if (fillStyle.Type == FillStyleType.RadialGradient)
        {
            RadialGradientFillStyleRGBA radialGradientFill = (RadialGradientFillStyleRGBA)fillStyle;
            ApplyColorSwapGradientRGBA(radialGradientFill.Gradient, colorSwapDict);
        }
        else if (fillStyle.Type == FillStyleType.FocalGradient)
        {
            FocalGradientFillStyleRGBA focalGradientFill = (FocalGradientFillStyleRGBA)fillStyle;
            ApplyColorSwapGradientRGBA(focalGradientFill.Gradient, colorSwapDict);
        }
    }

    private static void ApplyColorSwapGradientRGB(BaseGradientRGB gradient, Dictionary<uint, uint> colorSwapDict)
    {
        IList<GradientRecordRGB> gradientRecords = gradient.GradientRecords;
        GradientRecordRGB first = gradientRecords[0];
        first.Color = SwapSwfRGB(first.Color, colorSwapDict);
        GradientRecordRGB second = gradientRecords[1];
        second.Color = SwapSwfRGB(second.Color, colorSwapDict);
    }

    private static void ApplyColorSwapGradientRGBA(BaseGradientRGBA gradient, Dictionary<uint, uint> colorSwapDict)
    {
        IList<GradientRecordRGBA> gradientRecords = gradient.GradientRecords;
        GradientRecordRGBA first = gradientRecords[0];
        first.Color = SwapSwfRGBA(first.Color, colorSwapDict);
        GradientRecordRGBA second = gradientRecords[1];
        second.Color = SwapSwfRGBA(second.Color, colorSwapDict);
    }

    private static SwfRGB SwapSwfRGB(SwfRGB color, Dictionary<uint, uint> colorSwapDict)
    {
        uint colorHex = SwfRGBToHex(color);
        if (!colorSwapDict.TryGetValue(colorHex, out uint newColorHex))
            return color;
        SwfRGB newColor = HexToSwfRGB(newColorHex);
        return newColor;
    }

    private static SwfRGBA SwapSwfRGBA(SwfRGBA color, Dictionary<uint, uint> colorSwapDict)
    {
        uint colorHex = SwfRGBAToHex(color);
        if (!colorSwapDict.TryGetValue(colorHex, out uint newColorHex))
            return color;
        SwfRGBA newColor = HexToSwfRGBA(newColorHex);
        return newColor;
    }

    private static uint SwfRGBToHex(SwfRGB color)
    {
        return ((uint)color.Red << 16) | ((uint)color.Green << 8) | color.Blue;
    }

    // the game ignores the alpha
    private static uint SwfRGBAToHex(SwfRGBA color)
    {
        return ((uint)color.Red << 16) | ((uint)color.Green << 8) | color.Blue;
    }

    private static SwfRGB HexToSwfRGB(uint hex)
    {
        return new((byte)(hex >> 16), (byte)(hex >> 8), (byte)hex);
    }

    private static SwfRGBA HexToSwfRGBA(uint hex)
    {
        return new((byte)(hex >> 16), (byte)(hex >> 8), (byte)hex, (byte)(hex >> 24));
    }
}