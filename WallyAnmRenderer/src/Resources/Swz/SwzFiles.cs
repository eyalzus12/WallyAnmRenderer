using System;
using System.IO;

namespace WallyAnmRenderer;

public sealed class SwzFiles
{
    private string _brawlPath;
    public string BrawlPath
    {
        get => _brawlPath;
        set
        {
            _brawlPath = value;
            LoadSwzFiles();
        }
    }

    private uint _key;
    public uint Key
    {
        get => _key;
        set
        {
            _key = value;
            LoadSwzFiles();
        }
    }

    public SwzFiles(string brawlPath, uint key)
    {
        _brawlPath = brawlPath;
        _key = key;
        LoadSwzFiles();
    }

    public SwzInitFile? Init { get; private set; }
    public SwzGameFile? Game { get; private set; }

    public void LoadSwzFiles()
    {
        try
        {
            Init = new(Path.Combine(_brawlPath, "Init.swz"), _key);
            Game = new(Path.Combine(_brawlPath, "Game.swz"), _key);
        }
        catch (Exception e)
        {
            Rl.TraceLog(Raylib_cs.TraceLogLevel.Error, e.Message);
            Rl.TraceLog(Raylib_cs.TraceLogLevel.Trace, e.StackTrace);
        }
    }
}