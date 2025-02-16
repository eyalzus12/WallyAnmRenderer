using System;
using System.Collections.Generic;
using BrawlhallaAnimLib;
using BrawlhallaAnimLib.Bones;
using BrawlhallaAnimLib.Gfx;
using BrawlhallaAnimLib.Math;
using BrawlhallaAnimLib.Swf;
using Raylib_cs;

namespace WallyAnmRenderer;

public sealed class Animator(string brawlPath, uint key)
{
    public string BrawlPath { get => Loader.BrawlPath; set => Loader.BrawlPath = value; }
    public uint Key { get => Loader.Key; set => Loader.Key = value; }

    public Loader Loader { get; } = new(brawlPath, key);

    private readonly HashSet<(string, string)> _h = [];

    public bool Animate(IGfxType gfx, string animation, long frame, Transform2D transform)
    {
        Loader.AssetLoader.Upload();

        BoneSpriteWithName[]? sprites = AnimationBuilder.BuildAnim(Loader, gfx, animation, frame, transform);
        if (sprites is null) return false;

        bool result = true;
        foreach (BoneSpriteWithName sprite in sprites)
        {
            BoneShape[]? shapes = SpriteToShapeConverter.ConvertToShapes(Loader, sprite);
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
                Texture2DWrapper? textureWrapper = Loader.AssetLoader.LoadShapeFromSwf(
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