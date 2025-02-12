using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using WallyAnmSpinzor;

namespace WallyAnmRenderer;

public sealed class AssetLoader
{
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

    private void LoadAnmInThread(string name)
    {
        Task.Run(() =>
        {
            string anmPath = Path.Combine(_brawlPath, "anims", $"Animation_{name}.anm");
            AnmFile anm;
            using (FileStream file = File.OpenRead(anmPath))
                anm = AnmFile.CreateFrom(file);
            foreach ((string className, AnmClass @class) in anm.Classes)
            {
                AnmClasses[className] = @class;

                Console.WriteLine($"Anim class {className} has the following animations:");
                foreach (string animation in @class.Animations.Keys)
                {
                    Console.WriteLine($"    {animation}");
                }
            }
            Console.WriteLine($"Loaded {name}");
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

    public Texture2DWrapper? LoadShapeFromSwf(string filePath, ushort shapeId, double animScale, Dictionary<uint, uint> colorSwapDict, string boneName)
    {
        SwfFileData? swf = LoadSwf(filePath);
        if (swf is null)
            return null;
        SwfShapeCache.TryGetCached(boneName, shapeId, animScale, out Texture2DWrapper? texture);
        if (texture is not null)
            return texture;
        SwfShapeCache.LoadInThread(swf, shapeId, animScale, colorSwapDict, boneName);
        return null;
    }

    public const int MAX_TEXTURE_UPLOADS_PER_FRAME = 5;
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
        AnmClasses.Clear();
        LoadAnms();
    }

    public void LoadAnms()
    {
        LoadAnmInThread("CharacterSelect");
    }
}