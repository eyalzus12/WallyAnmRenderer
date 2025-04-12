using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
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

public sealed class Loader : ILoader
{
    private CancellationTokenSource? _loadSwzToken = null;

    private string _brawlPath;
    public string BrawlPath
    {
        get => AssetLoader.BrawlPath;
        set
        {
            _brawlPath = value;
            AssetLoader.BrawlPath = value;
        }
    }

    public uint Key { get; set; }

    public AssetLoader AssetLoader { get; }
    public SwzFiles? SwzFiles { get; private set; } = null;
    public LangFile? LangFile { get; private set; } = null;

    public Loader(string brawlPath, uint key)
    {
        _brawlPath = brawlPath;
        Key = key;

        AssetLoader = new(brawlPath);
    }

    public async Task LoadFilesAsync(CancellationToken cancellationToken = default)
    {
        await LoadSwzAsync(cancellationToken);
        LoadLang();
    }

    public async Task LoadSwzAsync(CancellationToken cancellationToken = default)
    {
        CancelLoadSwz();
        CancellationTokenSource source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _loadSwzToken = source;
        SwzFiles = await SwzFiles.NewAsync(_brawlPath, Key, source.Token);
    }

    public void LoadLang()
    {
        LangFile = LangFile.Load(Path.Join(_brawlPath, "languages", "language.1.bin"));
    }

    private void CancelLoadSwz()
    {
        if (_loadSwzToken is null) return;
        _loadSwzToken.Cancel();
        _loadSwzToken = null;
    }

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
            if (SwzFiles?.Init is null)
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
            if (SwzFiles?.Init is null)
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
        if (LangFile is null)
        {
            stringName = null;
            return false;
        }

        return LangFile.Entries.TryGetValue(stringKey, out stringName);
    }
}