using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
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

    public bool SwfExists(string swfPath)
    {
        return File.Exists(Path.Join(_brawlPath, swfPath));
    }

    public Task<IAnmClass?> GetAnmClass(string classIdentifier)
    {
        AnmClassAdapter? impl()
        {
            if (AssetLoader.AnmFileCache.TryGetAnmClass(classIdentifier, out AnmClass? @class))
                return new(@class);
            return null;
        }

        return Task.FromResult<IAnmClass?>(impl());
    }

    public Task<string?> GetBoneName(short boneId)
    {
        string? impl()
        {
            if (SwzFiles.Init is null)
                return null;

            BoneTypes boneTypes = SwzFiles.Init.BoneTypes;
            if (boneId <= 0 || boneId > boneTypes.BoneCount)
                return null;

            return boneTypes[boneId - 1];
        }

        return Task.FromResult(impl());
    }

    public Task<string?> GetBoneFilePath(string boneName)
    {
        string? impl()
        {
            if (SwzFiles.Init is null)
                return null;

            BoneSources boneSources = SwzFiles.Init.BoneSources;
            if (boneSources.TryGetBoneFilePath(boneName, out string? bonePath))
                return bonePath;
            return null;
        }

        return Task.FromResult(impl());
    }

    public async Task<uint[]?> GetScriptAVar(string swfPath, string spriteName)
    {
        SwfFileData swf = await AssetLoader.LoadSwf(swfPath);
        return swf.SpriteA.GetValueOrDefault(spriteName);
    }

    public async Task<ushort?> GetSymbolId(string swfPath, string symbolName)
    {
        SwfFileData swf = await AssetLoader.LoadSwf(swfPath);
        if (swf.SymbolClass.TryGetValue(symbolName, out ushort symbolId))
            return symbolId;
        return null;
    }

    public async Task<SwfTagBase?> GetTag(string swfPath, ushort tagId)
    {
        SwfFileData swf = await AssetLoader.LoadSwf(swfPath);
        if (swf.SpriteTags.TryGetValue(tagId, out DefineSpriteTag? sprite))
            return sprite;
        if (swf.ShapeTags.TryGetValue(tagId, out ShapeBaseTag? shape))
            return shape;
        if (swf.TextTags.TryGetValue(tagId, out DefineTextBaseTag? text))
            return text;
        return null;
    }

    public bool TryGetStringName(string stringKey, [MaybeNullWhen(false)] out string stringName)
    {
        return LangFile.Entries.TryGetValue(stringKey, out stringName);
    }
}