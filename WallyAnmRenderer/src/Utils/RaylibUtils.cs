using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BrawlhallaAnimLib.Math;
using Raylib_cs;
using SkiaSharp;

namespace WallyAnmRenderer;

public class RaylibUtils
{
    public static void DrawTextureWithTransform(Texture2D texture, double x, double y, double w, double h, Transform2D trans, float tintR = 1, float tintG = 1, float tintB = 1, float tintA = 1)
    {
        Rl.BeginBlendMode(BlendMode.AlphaPremultiply);
        Rlgl.SetTexture(texture.Id);
        Rlgl.Begin(DrawMode.Quads);
        Rlgl.Color4f(tintR * tintA, tintG * tintA, tintB * tintA, tintA);
        (double xMin, double yMin) = (x, y);
        (double xMax, double yMax) = (x + w, y + h);
        (double, double)[] texCoords = [(0, 0), (0, 1), (1, 1), (1, 0), (0, 0)];
        (double, double)[] points = [trans * (xMin, yMin), trans * (xMin, yMax), trans * (xMax, yMax), trans * (xMax, yMin), trans * (xMin, yMin)];
        // raylib requires that the points be in counterclockwise order
        if (IsPolygonClockwise(points))
        {
            Array.Reverse(texCoords);
            Array.Reverse(points);
        }
        for (int i = 0; i < points.Length - 1; ++i)
        {
            Rlgl.TexCoord2f((float)texCoords[i].Item1, (float)texCoords[i].Item2);
            Rlgl.Vertex2f((float)points[i].Item1, (float)points[i].Item2);
            Rlgl.TexCoord2f((float)texCoords[i + 1].Item1, (float)texCoords[i + 1].Item2);
            Rlgl.Vertex2f((float)points[i + 1].Item1, (float)points[i + 1].Item2);
        }
        Rlgl.End();
        Rlgl.SetTexture(0);
        Rl.EndBlendMode();
    }

    public static bool IsPolygonClockwise((double, double)[] poly)
    {
        double area = 0;
        for (int i = 0; i < poly.Length; ++i)
        {
            int j = (i + 1) % poly.Length;
            (double x1, double y1) = poly[i];
            (double x2, double y2) = poly[j];
            area += Cross(x1, y1, x2, y2);
        }
        return area > 0;
    }

    public static double Cross(double X1, double Y1, double X2, double Y2) => X1 * Y2 - X2 * Y1;

    /// <remarks>
    /// WARNING! Do not try to access the bitmap after calling this function.
    /// </remarks>
    [SkipLocalsInit]
    public static unsafe RlImage SKBitmapToRlImage(SKBitmap bitmap)
    {
        if (bitmap.ColorType != SKColorType.Rgba8888)
        {
            throw new ArgumentException($"{nameof(SKBitmapToRlImage)} only supports Rgba8888, but got {bitmap.ColorType}");
        }

        RlImage image = new()
        {
            Data = (void*)bitmap.GetPixels(),
            Width = bitmap.Width,
            Height = bitmap.Height,
            Mipmaps = 1,
            Format = PixelFormat.UncompressedR8G8B8A8,
        };

        // very evil. gaslight the bitmap into thinking it disposed its pixels.
        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = nameof(set_Handle))]
        extern static void set_Handle(SKObject obj, IntPtr handle);
        set_Handle(bitmap, IntPtr.Zero);

        return image;
    }

    public static Vector3 RlColorToVector3(RlColor color)
    {
        return new(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f);
    }

    public static RlColor Vector3ToRlColor(Vector3 color)
    {
        return new((byte)(color.X * 255), (byte)(color.Y * 255), (byte)(color.Z * 255));
    }
}