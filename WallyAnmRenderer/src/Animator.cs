using BrawlhallaAnimLib;
using BrawlhallaAnimLib.Bones;
using BrawlhallaAnimLib.Gfx;
using BrawlhallaAnimLib.Math;
using BrawlhallaAnimLib.Swf;
using Raylib_cs;

namespace WallyAnmRenderer;

public class Animator
{
    private readonly Loader _loader;
    private readonly AnimationBuilder _builder;
    private readonly SpriteToShapeConverter _converter;

    public Animator(string brawlPath, uint key)
    {
        _loader = new(brawlPath, key);
        _builder = new(_loader);
        _converter = new(_loader);
    }

    public bool Animate(IGfxType gfx, string animation, long frame, Transform2D transform)
    {
        _loader.AssetLoader.Upload();

        BoneSpriteWithName[]? sprites = _builder.BuildAnim(gfx, animation, frame, transform);
        if (sprites is null) return false;

        bool result = true;
        foreach (BoneSpriteWithName sprite in sprites)
        {
            BoneShape[]? shapes = _converter.ConvertToShapes(sprite);
            if (shapes is null)
            {
                result = false;
                continue;
            }

            foreach (BoneShape boneShape in shapes)
            {
                Texture2DWrapper? textureWrapper = _loader.AssetLoader.LoadShapeFromSwf(boneShape.SwfFilePath, boneShape.ShapeId, boneShape.AnimScale, boneShape.ColorSwapDict);
                if (textureWrapper is null)
                {
                    result = false;
                    continue;
                }

                Texture2D texture = textureWrapper.Texture;
                Transform2D textureTransform = textureWrapper.Transform;

                RaylibUtils.DrawTextureWithTransform(
                    texture,
                    0, 0, texture.Width, texture.Height,
                    boneShape.Transform * textureTransform,
                    tintA: (float)boneShape.Opacity
                );
            }
        }

        return result;
    }
}