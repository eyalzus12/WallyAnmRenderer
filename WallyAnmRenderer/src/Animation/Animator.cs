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

    public Animator(string brawlPath, uint key)
    {
        _loader = new(brawlPath, key);
    }

    private readonly HashSet<(string, string)> _h = [];

    public bool Animate(IGfxType gfx, string animation, long frame, Transform2D transform)
    {
        _loader.AssetLoader.Upload();

        BoneSpriteWithName[]? sprites = AnimationBuilder.BuildAnim(_loader, gfx, animation, frame, transform);
        if (sprites is null) return false;

        bool result = true;
        foreach (BoneSpriteWithName sprite in sprites)
        {
            BoneShape[]? shapes = SpriteToShapeConverter.ConvertToShapes(_loader, sprite);
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
                    sprite.SwfFilePath,
                    sprite.SpriteName,
                    boneShape.ShapeId,
                    sprite.AnimScale,
                    sprite.ColorSwapDict
                );

                if (textureWrapper is null)
                {
                    result = false;
                    continue;
                }

                Texture2D texture = textureWrapper.Texture;
                Transform2D drawTransform = boneShape.Transform * textureWrapper.Transform;

                RaylibUtils.DrawTextureWithTransform(
                    texture,
                    0, 0, texture.Width, texture.Height,
                    drawTransform,
                    tintA: (float)sprite.Opacity
                );
            }
        }

        return result;
    }
}