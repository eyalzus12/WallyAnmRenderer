using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using WallyAnmSpinzor;
using WallyAnmSpinzor.Version_904;

namespace WallyAnmRenderer;

public enum AnmFormatEnum
{
    Current,
    Pre904,
}

public sealed class AnmFileCache : ManagedCache<string, AnmFile>
{
    private readonly ConcurrentDictionary<string, ConcurrentBag<string>> _classOwnership = [];
    private readonly ConcurrentDictionary<string, AnmClass> _classes = [];

    public AnmFormatEnum AnmFormat { get; set; } = AnmFormatEnum.Current;

    public bool TryGetAnmClass(string name, [MaybeNullWhen(false)] out AnmClass @class)
    {
        return _classes.TryGetValue(name, out @class);
    }

    protected override async Task<AnmFile> LoadInternal(string path, CancellationToken ctoken = default)
    {
        AnmFile anm;
        using (FileStream file = FileUtils.OpenReadAsyncSeq(path))
        {
            switch (AnmFormat)
            {
                case AnmFormatEnum.Pre904:
                    AnmFile_904 old = await AnmFile_904.CreateFromAsync(file, false, ctoken);
                    anm = Anm904Migrator.MigrateFile(old);
                    break;
                case AnmFormatEnum.Current:
                default:
                    anm = await AnmFile.CreateFromAsync(file, false, ctoken);
                    break;
            }
        }

        ConcurrentBag<string> bag = _classOwnership.GetOrAdd(path, []);
        foreach ((string name, AnmClass @class) in anm.Classes)
        {
            ctoken.ThrowIfCancellationRequested();
            bag.Add(name);
            _classes[name] = @class;
        }

        return anm;
    }

    protected override void OnCacheClear()
    {
        base.OnCacheClear();
        _classOwnership.Clear();
        _classes.Clear();
    }

    protected override void OnRemoveCached(string filePath)
    {
        base.OnRemoveCached(filePath);
        if (!_classOwnership.TryGetValue(filePath, out ConcurrentBag<string>? bag)) return;
        foreach (string name in bag) _classes.Remove(name, out _);
        bag.Clear();
    }
}