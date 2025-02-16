using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using BrawlhallaAnimLib;
using BrawlhallaAnimLib.Anm;
using SwfLib.Tags;
using SwfLib.Tags.ShapeTags;
using WallyAnmSpinzor;

namespace WallyAnmRenderer;

public sealed class Loader(string brawlPath, uint key) : ILoader
{
    private string _brawlPath = brawlPath;
    public string BrawlPath
    {
        get => _brawlPath;
        set
        {
            _brawlPath = value;
            AssetLoader.BrawlPath = value;
            SwzFiles.BrawlPath = value;
        }
    }

    private uint _key = key;
    public uint Key
    {
        get => _key;
        set
        {
            _key = value;
            SwzFiles.Key = value;
        }
    }

    public SwzFiles SwzFiles { get; } = new(brawlPath, key);
    public AssetLoader AssetLoader { get; } = new(brawlPath);

    public void ClearCache()
    {
        AssetLoader.ClearCache();
    }

    public bool LoadBoneTypes()
    {
        return true;
    }

    public bool LoadBoneSources()
    {
        return true;
    }

    public bool SwfExists(string swfPath)
    {
        return File.Exists(Path.Join(_brawlPath, swfPath));
    }

    public bool LoadSwf(string swfPath)
    {
        return AssetLoader.LoadSwf(swfPath) is not null;
    }

    public bool LoadAnms()
    {
        return AssetLoader.AnmLoadingFinished;
    }

    public bool TryGetAnmClass(string classIdentifier, [MaybeNullWhen(false)] out IAnmClass anmClass)
    {
        if (AssetLoader.AnmClasses.TryGetValue(classIdentifier, out AnmClass? @class))
        {
            anmClass = new AnmClassAdapter(@class);
            return true;
        }
        anmClass = null;
        return false;
    }

    public bool TryGetBoneName(short boneId, [MaybeNullWhen(false)] out string boneName)
    {
        BoneTypes boneTypes = SwzFiles.Init.BoneTypes;
        boneId--;
        if (boneId < 0 || boneId >= boneTypes.BoneCount)
        {
            boneName = null;
            return false;
        }
        boneName = boneTypes[boneId];
        return true;
    }

    public bool TryGetBoneFilePath(string boneName, [MaybeNullWhen(false)] out string bonePath)
    {
        BoneSources boneSources = SwzFiles.Init.BoneSources;
        return boneSources.TryGetBoneFilePath(boneName, out bonePath);
    }

    public bool TryGetScriptAVar(string swfPath, string spriteName, [MaybeNullWhen(false)] out uint[] a)
    {
        SwfFileData? swf = AssetLoader.LoadSwf(swfPath);
        if (swf is null)
        {
            a = null;
            return false;
        }

        Dictionary<string, uint[]> dict = swf.SpriteA;
        return dict.TryGetValue(spriteName, out a);
    }

    public bool TryGetSymbolId(string swfPath, string symbolName, out ushort symbolId)
    {
        SwfFileData? swf = AssetLoader.LoadSwf(swfPath);
        if (swf is null)
        {
            symbolId = default;
            return false;
        }

        if (swf.SymbolClass.TryGetValue(symbolName, out symbolId))
        {
            return true;
        }

        symbolId = default;
        return false;
    }

    public bool TryGetTag(string swfPath, ushort tagId, [MaybeNullWhen(false)] out SwfTagBase tag)
    {
        SwfFileData? swf = AssetLoader.LoadSwf(swfPath);
        if (swf is null)
        {
            tag = null;
            return false;
        }

        if (swf.SpriteTags.TryGetValue(tagId, out DefineSpriteTag? sprite))
        {
            tag = sprite;
            return true;
        }

        if (swf.ShapeTags.TryGetValue(tagId, out ShapeBaseTag? shape))
        {
            tag = shape;
            return true;
        }

        tag = null;
        return false;
    }
}