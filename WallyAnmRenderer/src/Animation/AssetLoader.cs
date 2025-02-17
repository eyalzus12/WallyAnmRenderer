using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using WallyAnmSpinzor;

namespace WallyAnmRenderer;

public sealed class AssetLoader
{
    public bool AnmLoadingFinished { get; private set; }

    private string _brawlPath;
    public string BrawlPath
    {
        get => _brawlPath;
        set
        {
            _brawlPath = value;
            ClearCache();
            ReloadAnmCache();
        }
    }

    public AssetLoader(string brawlPath)
    {
        _brawlPath = brawlPath;
        ReloadAnmCache();
    }

    public SwfFileCache SwfFileCache { get; } = new();
    public SwfShapeCache SwfShapeCache { get; } = new();
    public ConcurrentDictionary<string, AnmClass> AnmClasses { get; set; } = [];

    private async Task LoadAnmInThread(string name)
    {
        await Task.Run(async () =>
        {
            try
            {
                await Console.Out.WriteLineAsync($"Starting to load anm {name}");
                string anmPath = Path.Combine(_brawlPath, "anims", $"{name}.anm");
                AnmFile anm;
                using (FileStream file = File.OpenRead(anmPath))
                    anm = AnmFile.CreateFrom(file);
                await Console.Out.WriteLineAsync($"Loaded anm {name}");
                foreach ((string className, AnmClass @class) in anm.Classes)
                {
                    AnmClasses[className] = @class;
                }
                await Console.Out.WriteLineAsync($"Finished loading anm {name}");
            }
            catch (Exception e)
            {
                Rl.TraceLog(Raylib_cs.TraceLogLevel.Error, e.Message);
                Rl.TraceLog(Raylib_cs.TraceLogLevel.Trace, e.StackTrace);
                throw;
            }
        });
    }

    public SwfFileData? LoadSwf(string filePath)
    {
        string finalPath = Path.GetFullPath(Path.Combine(_brawlPath, filePath));
        SwfFileCache.Cache.TryGetValue(finalPath, out SwfFileData? swf);
        if (swf is not null)
            return swf;
        SwfFileCache.LoadInThread(finalPath);
        return null;
    }

    public Texture2DWrapper? LoadShapeFromSwf(string filePath, string spriteName, ushort shapeId, double animScale, Dictionary<uint, uint> colorSwapDict)
    {
        SwfFileData? swf = LoadSwf(filePath);
        if (swf is null)
            return null;
        SwfShapeCache.TryGetCached(spriteName, shapeId, animScale, out Texture2DWrapper? texture);
        if (texture is not null)
            return texture;
        SwfShapeCache.LoadInThread(swf, spriteName, shapeId, animScale, colorSwapDict);
        return null;
    }

    public const int MAX_SWF_TEXTURE_UPLOADS_PER_FRAME = 5;
    public void Upload()
    {
        SwfShapeCache.Upload(MAX_SWF_TEXTURE_UPLOADS_PER_FRAME);
    }

    public void ClearCache()
    {
        SwfShapeCache.Clear();
        SwfFileCache.Clear();
    }

    public void ReloadAnmCache()
    {
        AnmLoadingFinished = false;
        AnmClasses.Clear();
        _ = LoadAnms();
    }

    public async Task LoadAnms()
    {
        await LoadAnmInThread("Animation_CharacterSelect");
        AnmLoadingFinished = true;
    }
}