using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace WallyAnmRenderer;

public readonly record struct SwfOverride(string OverridePath, string OriginalPath, SwfFileData Data);

public sealed class SwfOverrideMap(string brawlPath)
{
    public string BrawlPath { get; set; } = brawlPath;
    private readonly Dictionary<string, SwfOverride> _swfs = [];

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
        return _swfs.Remove(relativePath);
    }

    public IEnumerable<SwfOverride> Overrides => _swfs.Values;

    private string GetRelativePath(string fullPath)
    {
        return Path.GetRelativePath(BrawlPath, fullPath).Replace('\\', '/');
    }
}