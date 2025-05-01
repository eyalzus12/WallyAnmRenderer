using System.Collections.Generic;
using System.Threading.Tasks;
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

    public async Task<BoneSpriteWithName[]> GetAnimationInfo(IGfxType gfx, string animation, long frame, Transform2D transform)
    {
        List<BoneSpriteWithName> result = [];
        await foreach (BoneSpriteWithName sprite in AnimationBuilder.BuildAnim(Loader, gfx, animation, frame, transform))
        {
            result.Add(sprite);
        }
        return [.. result];
    }

    public ValueTask<BoneShape[]> SpriteToShapes(BoneSpriteWithName sprite)
    {
        return SpriteToShapeConverter.ConvertToShapes(Loader, sprite);
    }

    public Texture2DWrapper? ShapeToTexture(BoneSpriteWithName sprite, BoneShape shape)
    {
        return Loader.AssetLoader.LoadShapeFromSwf(
            sprite.SwfFilePath,
            sprite.SpriteName,
            shape.ShapeId,
            sprite.AnimScale,
            sprite.ColorSwapDict
        );
    }

    public async ValueTask<long> GetFrameCount(GfxInfo info)
    {
        if (!info.AnimationPicked)
            throw new System.Exception();
        return await AnimationBuilder.GetAnimFrameCount(Loader, info.AnimFile, info.AnimClass, info.Animation);
    }
}