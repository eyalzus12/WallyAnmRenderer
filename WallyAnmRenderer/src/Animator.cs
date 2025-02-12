using System;
using System.Collections.Generic;
using BrawlhallaAnimLib;
using BrawlhallaAnimLib.Bones;
using BrawlhallaAnimLib.Gfx;
using BrawlhallaAnimLib.Math;
using BrawlhallaAnimLib.Swf;
using Raylib_cs;

namespace WallyAnmRenderer;

public sealed class Animator
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

    private readonly HashSet<(string, string)> _h = [];

    public bool Animate(IGfxType gfx, string animation, long frame, Transform2D transform)
    {
        _loader.AssetLoader.Upload();

        BoneSpriteWithName[]? sprites = _builder.BuildAnim(gfx, animation, frame, transform);
        if (sprites is null) return false;

        bool result = true;
        foreach (BoneSpriteWithName sprite in sprites)
        {
            BoneShape[]? shapes = _converter.ConvertToShapes(sprite, frame);
            if (shapes is null)
            {
                result = false;
                continue;
            }

            if (!_h.Contains((sprite.SpriteName, sprite.SwfFilePath)))
            {
                Console.WriteLine($"Loading {sprite.SpriteName} from {sprite.SwfFilePath}");
                _h.Add((sprite.SpriteName, sprite.SwfFilePath));
            }

            foreach (BoneShape boneShape in shapes)
            {
                Texture2DWrapper? textureWrapper = _loader.AssetLoader.LoadShapeFromSwf(
                    boneShape.SwfFilePath,
                    boneShape.ShapeId,
                    boneShape.AnimScale,
                    boneShape.ColorSwapDict,
                    boneShape.BoneName
                );

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