using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using WallyAnmSpinzor;

namespace WallyAnmRenderer;

public sealed class AnmFileCache : ManagedCache<string, AnmFile>
{
    private readonly ConcurrentDictionary<string, AnmClass> _classes = [];

    public bool TryGetAnmClass(string name, [MaybeNullWhen(false)] out AnmClass @class)
    {
        return _classes.TryGetValue(name, out @class);
    }

    protected override AnmFile LoadInternal(string path)
    {
        using FileStream file = File.OpenRead(path);
        AnmFile anm = AnmFile.CreateFrom(file);

        foreach ((string name, AnmClass @class) in anm.Classes)
            _classes[name] = @class;

        return anm;
    }

    protected override void OnCacheClear()
    {
        base.OnCacheClear();
        _classes.Clear();
    }

    protected override void OnRemoveCached(string filePath, AnmFile anm)
    {
        base.OnRemoveCached(filePath, anm);
        foreach (string name in anm.Classes.Keys)
            _classes.Remove(name, out _);
    }
}