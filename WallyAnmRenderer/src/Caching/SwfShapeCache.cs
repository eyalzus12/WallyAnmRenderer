using System;
using System.Xml;

using SwiffCheese.Shapes;
using SwiffCheese.Exporting.Svg;

using Raylib_cs;

using Svg.Skia;
using SkiaSharp;
using BrawlhallaAnimLib.Math;
using SwfLib.Tags.ShapeTags;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace WallyAnmRenderer;

public sealed class SwfShapeCache : UploadCache<SwfShapeCache.TextureInfo, SwfShapeCache.ShapeData, Texture2DWrapper>
{
    // this is how the game checks the cache.
    // AnimScale only matters for digits.
    // (deviation from the game: we check the shapeId. this is to handle animations correctly.)
    private sealed class TextureInfoHasher : IEqualityComparer<TextureInfo>
    {
        private static string GetTrueSpriteName(TextureInfo texture)
        {
            if (!texture.SpriteName.StartsWith("a_Digit")) return texture.SpriteName;
            return texture.SpriteName + Math.Round(texture.AnimScale * ANIM_SCALE_MULTIPLIER, 2).ToString();
        }

        public bool Equals(TextureInfo x, TextureInfo y)
        {
            return x.ShapeId == y.ShapeId && GetTrueSpriteName(x) == GetTrueSpriteName(y);
        }

        public int GetHashCode(TextureInfo obj)
        {
            return HashCode.Combine(GetTrueSpriteName(obj), obj.ShapeId);
        }
    }

    private const int SWF_UNIT_DIVISOR = 20;
    private const double ANIM_SCALE_MULTIPLIER = 1.2;

    public readonly record struct TextureInfo(SwfFileData Swf, string SpriteName, ushort ShapeId, double AnimScale, Dictionary<uint, uint> ColorSwapDict);
    public readonly record struct ShapeData(RlImage Img, Transform2D Transform);

    protected override IEqualityComparer<TextureInfo>? KeyEqualityComparer { get; } = new TextureInfoHasher();

    protected override ShapeData LoadIntermediate(TextureInfo textureInfo)
    {
        (SwfFileData swf, _, ushort shapeId, double animScale, Dictionary<uint, uint> colorSwapDict) = textureInfo;
        animScale *= ANIM_SCALE_MULTIPLIER;
        ShapeBaseTag shape = swf.ShapeTags[shapeId];

        shape = SwfUtils.DeepCloneShape(shape);
        ColorSwapUtils.ApplyColorSwaps(shape, colorSwapDict);

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
        // 20 seems to work, but does it? it's also expensive...
        using SKBitmap bitmap1 = svg.Picture!.ToBitmap(SKColors.Transparent, 20, 20, SKColorType.Rgba8888, SKAlphaType.Premul, SKColorSpace.CreateSrgb())!;
        svg.Dispose();
        // Medium and High work the same for downscaling
        using SKBitmap bitmap2 = bitmap1.Resize(new SKSizeI(imageW, imageH), SKFilterQuality.Medium);
        bitmap1.Dispose();
        RlImage img = RaylibUtils.SKBitmapToRlImage(bitmap2);
        bitmap2.Dispose();

        // no need for alpha premult since we specify it in the ToBitmap

        Transform2D.Invert(transform, out Transform2D inv);
        return new ShapeData(img, inv);
    }

    protected override Texture2DWrapper IntermediateToValue(ShapeData shapeData)
    {
        (RlImage img, Transform2D trans) = shapeData;
        Texture2D texture = Rl.LoadTextureFromImage(img);
        return new(texture, trans);
    }

    protected override void UnloadIntermediate(ShapeData shapeData)
    {
        Rl.UnloadImage(shapeData.Img);
    }

    protected override void UnloadValue(Texture2DWrapper texture)
    {
        texture.Dispose();
    }

    public void Load(SwfFileData swf, string spriteName, ushort shapeId, double animScale, Dictionary<uint, uint> colorSwapDict) => Load(new(swf, spriteName, shapeId, animScale, colorSwapDict));
    public void LoadInThread(SwfFileData swf, string spriteName, ushort shapeId, double animScale, Dictionary<uint, uint> colorSwapDict) => LoadInThread(new(swf, spriteName, shapeId, animScale, colorSwapDict));

    public bool TryGetCached(string spriteName, ushort shapeId, double animScale, [MaybeNullWhen(false)] out Texture2DWrapper? texture)
    {
        TextureInfo fake = new(null!, spriteName, shapeId, animScale, null!);
        return TryGetCached(fake, out texture);
    }
}