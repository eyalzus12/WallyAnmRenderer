using System;
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

    public static RlImage SKBitmapToRlImage(SKBitmap bitmap)
    {
        if (bitmap.ColorType != SKColorType.Rgba8888)
        {
            throw new ArgumentException($"{nameof(SKBitmapToRlImage)} only supports Rgba8888, but got {bitmap.ColorType}");
        }

        unsafe
        {
            // use Rl alloc so GC doesn't free the memory
            void* bufferPtr = Rl.MemAlloc((uint)bitmap.ByteCount);
            // create a Span from the unmanaged memory
            Span<byte> buffer = new(bufferPtr, bitmap.ByteCount);
            // copy the bitmap bytes to the span
            bitmap.GetPixelSpan().CopyTo(buffer);

            return new()
            {
                Data = bufferPtr,
                Width = bitmap.Width,
                Height = bitmap.Height,
                Mipmaps = 1,
                Format = PixelFormat.UncompressedR8G8B8A8,
            };
        }
    }
}