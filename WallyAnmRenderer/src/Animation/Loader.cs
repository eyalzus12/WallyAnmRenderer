using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using SwfLib.Tags;
using SwfLib.Tags.ShapeTags;
using SwfLib.Tags.TextTags;
using BrawlhallaAnimLib;
using BrawlhallaAnimLib.Anm;
using BrawlhallaLangReader;
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

    public uint Key { get => SwzFiles.Key; set => SwzFiles.Key = value; }

    public AssetLoader AssetLoader { get; } = new(brawlPath);
    public SwzFiles SwzFiles { get; } = new(brawlPath, key);
    public LangFile LangFile { get; } = LangFile.Load(Path.Join(brawlPath, "languages", "language.1.bin"));

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

    public bool TryGetAnmClass(string classIdentifier, [MaybeNullWhen(false)] out IAnmClass anmClass)
    {
        if (AssetLoader.AnmFileCache.TryGetAnmClass(classIdentifier, out AnmClass? @class))
        {
            anmClass = new AnmClassAdapter(@class);
            return true;
        }
        anmClass = null;
        return false;
    }

    public bool TryGetBoneName(short boneId, [MaybeNullWhen(false)] out string boneName)
    {
        if (SwzFiles.Init is null)
        {
            boneName = null;
            return false;
        }

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
        if (SwzFiles.Init is null)
        {
            bonePath = null;
            return false;
        }

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

        if (swf.TextTags.TryGetValue(tagId, out DefineTextBaseTag? text))
        {
            tag = text;
            return true;
        }

        tag = null;
        return false;
    }

    public bool TryGetStringName(string stringKey, [MaybeNullWhen(false)] out string stringName)
    {
        return LangFile.Entries.TryGetValue(stringKey, out stringName);
    }
}