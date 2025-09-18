using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;

namespace WallyAnmRenderer;

public sealed class SwfOverrideMap(string brawlPath)
{
    public string BrawlPath { get; set; } = brawlPath;
    private readonly ConcurrentDictionary<string, SwfOverride> _swfs = [];

    public SwfOverride this[string fullPath]
    {
        get
        {
            string relativePath = GetRelativePath(fullPath);
            return _swfs[relativePath];
        }
        set
        {
            string relativePath = GetRelativePath(fullPath);
            _swfs[relativePath] = value;
        }
    }

    public bool TryGetValue(string fullPath, [MaybeNullWhen(false)] out SwfOverride swf)
    {
        string relativePath = GetRelativePath(fullPath);
        return _swfs.TryGetValue(relativePath, out swf);
    }

    public bool Remove(string fullPath)
    {
        string relativePath = GetRelativePath(fullPath);
        return _swfs.Remove(relativePath, out _);
    }

    public async Task ReloadOverride(string fullPath)
    {
        string relativePath = GetRelativePath(fullPath);
        if (_swfs.TryGetValue(relativePath, out SwfOverride swfOverride))
        {
            SwfOverride newOverride = await SwfOverride.Create(swfOverride.OriginalPath, swfOverride.OverridePath);
            _swfs.TryUpdate(relativePath, newOverride, swfOverride);
        }
    }

    public IEnumerable<SwfOverride> Overrides => _swfs.Values;

    private string GetRelativePath(string fullPath)
    {
        return Path.GetRelativePath(BrawlPath, fullPath).Replace('\\', '/');
    }
}