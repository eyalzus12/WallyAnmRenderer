using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using BrawlhallaAnimLib.Math;
using Raylib_cs;
using ImageMagick;

namespace WallyAnmRenderer;

public class TextureCache : UploadCache<TextureCache.SpriteData, (RlImage, Transform2D), Texture2DWrapper>
{
    protected override IEqualityComparer<SpriteData>? KeyEqualityComparer => new SpriteDataHasher();
    private sealed class SpriteDataHasher : IEqualityComparer<SpriteData>
    {
        public bool Equals(SpriteData x, SpriteData y)
        {
            return x.FilePath == y.FilePath;
        }

        public int GetHashCode(SpriteData obj)
        {
            return obj.FilePath.GetHashCode();
        }
    }

    public readonly record struct SpriteData(string FilePath, double OffsetX, double OffsetY);

    protected unsafe override (RlImage, Transform2D) LoadIntermediate(SpriteData spriteData)
    {
        using MagickImage mgImage = new(spriteData.FilePath);
        mgImage.Alpha(AlphaOption.On);
        mgImage.Alpha(AlphaOption.Associate);

        int width = (int)mgImage.Width;
        int height = (int)mgImage.Height;
        byte[] mgPixels = mgImage.GetPixels().ToByteArray(PixelMapping.RGBA)!;
        mgImage.Dispose();

        RlImage rlImage2;
        fixed (byte* mgPixelsPtr = mgPixels)
        {
            RlImage rlImage = new()
            {
                Data = mgPixelsPtr,
                Width = width,
                Height = height,
                Mipmaps = 1,
                Format = PixelFormat.UncompressedR8G8B8A8,
            };
            rlImage2 = RaylibEx.ImageCopyWithMipmaps(rlImage);
        }
        mgPixels = null!;

        return (rlImage2, Transform2D.CreateTranslate(spriteData.OffsetX, spriteData.OffsetY));
    }

    protected override Texture2DWrapper IntermediateToValue((RlImage, Transform2D) i)
    {
        (RlImage img, Transform2D transform) = i;
        Texture2D texture = Rl.LoadTextureFromImage(img);
        return new(texture, transform);
    }

    protected override void InitValue(Texture2DWrapper v)
    {
        Texture2D texture = v.Texture;
        Rl.SetTextureWrap(texture, TextureWrap.Clamp);
        Rl.GenTextureMipmaps(ref texture);
    }

    protected override void UnloadIntermediate((RlImage, Transform2D) i)
    {
        Rl.UnloadImage(i.Item1);
    }

    protected override void UnloadValue(Texture2DWrapper texture)
    {
        texture.Dispose();
    }

    public bool DidError(string filePath) => base.DidError(new(filePath, 0, 0));
    public bool TryGetCached(string filePath, [MaybeNullWhen(false)] out Texture2DWrapper? texture)
    {
        return base.TryGetCached(new(filePath, 0, 0), out texture);
    }
}