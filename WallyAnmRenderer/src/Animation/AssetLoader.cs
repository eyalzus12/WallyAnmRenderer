using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using BrawlhallaAnimLib.Gfx;
using WallyAnmSpinzor;

namespace WallyAnmRenderer;

public sealed class AssetLoader(string brawlPath)
{
    private string _brawlPath = brawlPath;
    public string BrawlPath
    {
        get => _brawlPath;
        set
        {
            if (_brawlPath == value) return;

            _brawlPath = value;
            _swfFileOverrides.BrawlPath = value;
            ClearSwfFileCache();
            ClearAnmCache();
            ClearSwfShapeCache();
        }
    }

    public AnmFileCache AnmFileCache { get; } = new();
    private readonly TextureCache _textureCache = new();
    private readonly SwfFileCache _swfFileCache = new();
    private readonly SwfShapeCache _swfShapeCache = new();
    private readonly SwfOverrideMap _swfFileOverrides = new(brawlPath);

    public bool IsAnmLoading(string filePath)
    {
        string finalPath = Path.GetFullPath(Path.Combine(_brawlPath, filePath));
        return AnmFileCache.IsLoading(finalPath);
    }

    public bool TryGetAnm(string filePath, [MaybeNullWhen(false)] out AnmFile anm)
    {
        string finalPath = Path.GetFullPath(Path.Combine(_brawlPath, filePath));
        return AnmFileCache.TryGetCached(finalPath, out anm);
    }

    public AnmFile? LoadAnm(string filePath)
    {
        string finalPath = Path.GetFullPath(Path.Combine(_brawlPath, filePath));
        ValueTask<AnmFile> task = AnmFileCache.LoadAsync(finalPath);
        if (task.IsCompletedSuccessfully)
            return task.Result;
        return null;
    }

    public ValueTask<SwfFileData> LoadSwf(string filePath)
    {
        string finalPath = Path.GetFullPath(Path.Combine(_brawlPath, filePath));
        if (_swfFileOverrides.TryGetValue(finalPath, out SwfOverride overrideData))
            return ValueTask.FromResult(overrideData.Data);
        return _swfFileCache.LoadAsync(finalPath);
    }

    public Texture2DWrapper? LoadTexture(ISpriteData spriteData)
    {
        string file = Path.GetFullPath(Path.Combine(_brawlPath, spriteData.File));
        _textureCache.TryGetCached(file, out Texture2DWrapper? texture);
        if (texture is not null)
            return texture;
        _textureCache.LoadInThread(new(file, spriteData.XOffset, spriteData.YOffset));
        if (_textureCache.DidError(file))
            return new();
        return null;
    }

    public Texture2DWrapper? LoadShapeFromSwf(string filePath, string spriteName, ushort shapeId, double animScale, Dictionary<uint, uint> colorSwapDict)
    {
        ValueTask<SwfFileData> task = LoadSwf(filePath);
        if (!task.IsCompleted)
            return null;
        else if (!task.IsCompletedSuccessfully)
            return new();
        SwfFileData swf = task.Result;
        _swfShapeCache.TryGetCached(spriteName, shapeId, animScale, out Texture2DWrapper? texture);
        if (texture is not null)
            return texture;
        _swfShapeCache.LoadInThread(swf, spriteName, shapeId, animScale, colorSwapDict);
        if (_swfShapeCache.DidError(swf, spriteName, shapeId, animScale, colorSwapDict))
            return new();
        return null;
    }

    public const int MAX_TEXTURE_UPLOADS_PER_FRAME = 5;
    public const int MAX_SWF_TEXTURE_UPLOADS_PER_FRAME = 5;
    public void Upload()
    {
        _textureCache.Upload(MAX_TEXTURE_UPLOADS_PER_FRAME);
        _swfShapeCache.Upload(MAX_SWF_TEXTURE_UPLOADS_PER_FRAME);
    }

    public void AddSwfOverride(string relativePath, SwfOverride data)
    {
        string fullPath = Path.GetFullPath(Path.Combine(_brawlPath, relativePath));
        _swfFileOverrides[fullPath] = data;
    }

    public bool RemoveSwfOverride(string relativePath)
    {
        string fullPath = Path.GetFullPath(Path.Combine(_brawlPath, relativePath));
        return _swfFileOverrides.Remove(fullPath);
    }

    public IEnumerable<SwfOverride> Overrides => _swfFileOverrides.Overrides;

    public void ClearTextureCache()
    {
        _textureCache.Clear();
    }

    public void ClearSwfShapeCache()
    {
        _swfShapeCache.Clear();
    }

    public void ClearSwfFileCache()
    {
        _swfFileCache.Clear();
    }

    public void ClearAnmCache()
    {
        AnmFileCache.Clear();
    }
}