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

    public SwzInitFile Init { get; private set; } = null!;
    public SwzGameFile Game { get; private set; } = null!;

    public void LoadSwzFiles()
    {
        Init = new(Path.Combine(_brawlPath, "Init.swz"), _key);
        Game = new(Path.Combine(_brawlPath, "Game.swz"), _key);
    }
}