using System;
using System.Xml;
using BrawlhallaAnimLib.Bones;
using BrawlhallaAnimLib.Math;
using Raylib_cs;
using SkiaSharp;
using Svg.Skia;
using SwfLib.Tags;
using SwfLib.Tags.ShapeTags;
using SwiffCheese.Exporting.Svg;
using SwiffCheese.Shapes;

namespace WallyAnmRenderer;

public class SwfShapeToTexture
{
    const int SWF_UNIT_DIVISOR = 20;
    const double ANIM_SCALE_MULTIPLIER = 1.2;

    public static (Texture2D, Transform2D) ToTexture(Loader loader, string swfPath, ushort shapeId, double animScale)
    {
        if (!loader.TryGetTag(swfPath, shapeId, out SwfTagBase? tag))
            throw new Exception();
        if (tag is not ShapeBaseTag shape)
            throw new Exception();

        animScale *= ANIM_SCALE_MULTIPLIER;

        SwfShape compiledShape = new(new(shape));
        // logic follows game
        int shapeX = shape.ShapeBounds.XMin;
        int shapeY = shape.ShapeBounds.YMin;
        int shapeW = shape.ShapeBounds.XMax - shape.ShapeBounds.XMin;
        int shapeH = shape.ShapeBounds.YMax - shape.ShapeBounds.YMin;

        double x = shapeX * 1.0 / SWF_UNIT_DIVISOR;
        double y = shapeY * 1.0 / SWF_UNIT_DIVISOR;
        double w = shapeW * animScale / SWF_UNIT_DIVISOR;
        double h = shapeH * animScale / SWF_UNIT_DIVISOR;

        int offsetX = (int)Math.Floor(x);
        int offsetY = (int)Math.Floor(y);
        int imageW = (int)Math.Floor(w + (x - offsetX) + animScale) + 2;
        int imageH = (int)Math.Floor(h + (y - offsetY) + animScale) + 2;

        Transform2D transform = Transform2D.CreateScale(animScale, animScale) * Transform2D.CreateTranslate(-offsetX, -offsetY);

        SvgSize size = new(imageW, imageH);
        SvgMatrix matrix = new(transform.ScaleX, transform.SkewY, transform.SkewX, transform.ScaleY, transform.TranslateX, transform.TranslateY);
        SvgShapeExporter exporter = new(size, matrix);
        compiledShape.Export(exporter);

        using XmlReader reader = exporter.Document.CreateReader();
        using SKSvg svg = SKSvg.CreateFromXmlReader(reader);
        reader.Dispose();
        using SKBitmap bitmap1 = svg.Picture!.ToBitmap(SKColors.Transparent, 20, 20, SKColorType.Rgba8888, SKAlphaType.Premul, SKColorSpace.CreateSrgb())!;
        svg.Dispose();
        // Medium and High work the same for downscaling
        using SKBitmap bitmap2 = bitmap1.Resize(new SKSizeI(imageW, imageH), SKFilterQuality.Medium);
        bitmap1.Dispose();
        RlImage img = SKBitmapToRlImage(bitmap2);
        bitmap2.Dispose();
        Texture2D texture = Rl.LoadTextureFromImage(img);
        Rl.UnloadImage(img);

        // no need for alpha premult since we specify it in the ToBitmap

        Transform2D.Invert(transform, out Transform2D inv);
        return (texture, inv);
    }

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