using System;
using System.Collections.Generic;
using BrawlhallaAnimLib;
using BrawlhallaAnimLib.Bones;
using BrawlhallaAnimLib.Gfx;
using BrawlhallaAnimLib.Math;
using BrawlhallaAnimLib.Swf;

namespace WallyAnmRenderer;

public sealed class Animator(string brawlPath, uint key)
{
    public string BrawlPath { get => Loader.BrawlPath; set => Loader.BrawlPath = value; }
    public uint Key { get => Loader.Key; set => Loader.Key = value; }

    public Loader Loader { get; } = new(brawlPath, key);

    private readonly HashSet<(string, string)> _h = [];

    public BoneSpriteWithName[]? GetAnimationInfo(IGfxType gfx, string animation, long frame, Transform2D transform)
    {
        return AnimationBuilder.BuildAnim(Loader, gfx, animation, frame, transform);
    }

    public Texture2DWrapper[]? SpriteToTextures(BoneSpriteWithName sprite)
    {
        BoneShape[]? shapes = SpriteToShapeConverter.ConvertToShapes(Loader, sprite);
        if (shapes is null)
            return null;

        if (!_h.Contains((sprite.SpriteName, sprite.SwfFilePath)))
        {
            Console.WriteLine($"Loading {sprite.SpriteName} from {sprite.SwfFilePath}");
            _h.Add((sprite.SpriteName, sprite.SwfFilePath));
        }

        bool result = true;
        List<Texture2DWrapper> textures = [];
        foreach (BoneShape boneShape in shapes)
        {
            Texture2DWrapper? texture = Loader.AssetLoader.LoadShapeFromSwf(
                sprite.SwfFilePath,
                sprite.SpriteName,
                boneShape.ShapeId,
                sprite.AnimScale,
                sprite.ColorSwapDict
            );

            if (texture is null)
            {
                result = false;
                continue;
            }

            textures.Add(new(texture.Texture, boneShape.Transform * texture.Transform, false));
        }

        if (result) return [.. textures];
        return null;
    }

    public long? GetFrameCount(GfxInfo info)
    {
        if (!info.AnimationPicked)
            return null;
        return AnimationBuilder.GetAnimFrameCount(Loader, info.AnimFile, info.AnimClass, info.Animation);
    }
}