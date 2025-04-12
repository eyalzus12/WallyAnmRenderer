using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace WallyAnmRenderer;

public sealed class SwzFiles
{
    private const string INIT_SWZ = "Init.swz";
    private const string GAME_SWZ = "Game.swz";

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

    private SwzFiles(string brawlPath, uint key)
    {
        _brawlPath = brawlPath;
        _key = key;
    }

    public static SwzFiles New(string brawlPath, uint key)
    {
        SwzFiles result = new(brawlPath, key);
        result.LoadSwzFiles();
        return result;
    }

    public static async Task<SwzFiles> NewAsync(string brawlPath, uint key, CancellationToken cancellationToken = default)
    {
        SwzFiles result = new(brawlPath, key);
        await result.LoadSwzFilesAsync(cancellationToken);
        return result;
    }

    public SwzInitFile? Init { get; private set; }
    public SwzGameFile? Game { get; private set; }

    public void LoadSwzFiles()
    {
        try
        {
            Init = SwzInitFile.New(Path.Combine(_brawlPath, INIT_SWZ), _key);
            Game = SwzGameFile.New(Path.Combine(_brawlPath, GAME_SWZ), _key);
        }
        catch (Exception e)
        {
            Rl.TraceLog(Raylib_cs.TraceLogLevel.Error, e.Message);
            Rl.TraceLog(Raylib_cs.TraceLogLevel.Trace, e.StackTrace);
        }
    }

    public async Task LoadSwzFilesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            async Task LoadInit(CancellationToken cancellationToken = default)
            {
                Init = await SwzInitFile.NewAsync(Path.Combine(_brawlPath, INIT_SWZ), _key, cancellationToken);
            }

            async Task LoadGame(CancellationToken cancellationToken = default)
            {
                Game = await SwzGameFile.NewAsync(Path.Combine(_brawlPath, GAME_SWZ), _key, cancellationToken);
            }

            await Task.WhenAll(LoadInit(cancellationToken), LoadGame(cancellationToken));
        }
        catch (Exception e)
        {
            if (e is not OperationCanceledException)
            {
                Rl.TraceLog(Raylib_cs.TraceLogLevel.Error, e.Message);
                Rl.TraceLog(Raylib_cs.TraceLogLevel.Trace, e.StackTrace);
            }

            throw;
        }
    }
}