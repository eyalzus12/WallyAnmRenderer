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

    public async Task<BoneSprite[]> GetAnimationInfo(IGfxType gfx, string animation, long frame, Transform2D transform)
    {
        List<BoneSprite> result = [];
        await foreach (BoneSprite sprite in AnimationBuilder.BuildAnim(Loader, gfx, animation, frame, transform))
        {
            result.Add(sprite);
        }
        return [.. result];
    }

    public ValueTask<BoneShape[]> SpriteToShapes(SwfBoneSprite sprite)
    {
        return SpriteToShapeConverter.ConvertToShapes(Loader, sprite);
    }

    public Texture2DWrapper? ShapeToTexture(SwfBoneSprite sprite, BoneShape shape)
    {
        if (sprite is not SwfBoneSpriteWithName spriteWithName)
            throw new System.ArgumentException("ShapeToTexture does not support sprites with id");

        return Loader.AssetLoader.LoadShapeFromSwf(
                sprite.SwfFilePath,
                spriteWithName.SpriteName,
                shape.ShapeId,
                sprite.AnimScale,
                sprite.ColorSwapDict
            );
    }

    public async ValueTask<AnimationData> GetAnimData(GfxInfo info)
    {
        if (!info.AnimationPicked)
            throw new System.Exception();
        return await AnimationBuilder.GetAnimData(Loader, info.AnimFile, info.AnimClass, info.Animation);
    }
}