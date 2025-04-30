using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
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
            ClearSwfFileCache();
            ClearAnmCache();
            ClearSwfShapeCache();
        }
    }

    public AnmFileCache AnmFileCache { get; } = new();
    private readonly SwfFileCache _swfFileCache = new();
    private readonly SwfShapeCache _swfShapeCache = new();

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
        ValueTask<AnmFile> task = AnmFileCache.LoadThreaded(finalPath);
        if (task.IsCompletedSuccessfully)
            return task.Result;
        return null;
    }

    public ValueTask<SwfFileData> LoadSwf(string filePath)
    {
        string finalPath = Path.GetFullPath(Path.Combine(_brawlPath, filePath));
        return _swfFileCache.LoadThreaded(finalPath);
    }

    public Texture2DWrapper? LoadShapeFromSwf(string filePath, string spriteName, ushort shapeId, double animScale, Dictionary<uint, uint> colorSwapDict)
    {
        ValueTask<SwfFileData> task = LoadSwf(filePath);
        if (!task.IsCompletedSuccessfully)
            return null;
        SwfFileData swf = task.Result;
        _swfShapeCache.TryGetCached(spriteName, shapeId, animScale, out Texture2DWrapper? texture);
        if (texture is not null)
            return texture;
        _swfShapeCache.LoadInThread(swf, spriteName, shapeId, animScale, colorSwapDict);
        return null;
    }

    public const int MAX_SWF_TEXTURE_UPLOADS_PER_FRAME = 5;
    public void Upload()
    {
        _swfShapeCache.Upload(MAX_SWF_TEXTURE_UPLOADS_PER_FRAME);
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