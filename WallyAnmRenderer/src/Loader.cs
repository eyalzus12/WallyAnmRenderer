using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using BrawlhallaAnimLib;
using BrawlhallaAnimLib.Anm;
using SwfLib.Tags;
using SwfLib.Tags.ShapeTags;
using WallyAnmSpinzor;

namespace WallyAnmRenderer;

public class Loader : ILoader
{
    private string _brawlPath;

    public Loader(string brawlPath, uint key)
    {
        _brawlPath = brawlPath;

        string init = Path.Join(_brawlPath, "Init.swz");
        string boneTypes = SwzUtils.GetFileFromSwz(init, key, "BoneTypes.xml") ?? throw new Exception();
        _boneTypes = [.. XElement.Parse(boneTypes).Elements("Bone").Select((ee) => ee.Value)];

        AssetLoader = new(brawlPath);
    }

    private readonly string[]? _boneTypes;
    public AssetLoader AssetLoader { get; }

    public bool IsBoneTypesLoaded() => _boneTypes is not null;
    public bool IsSwfLoaded(string swfPath)
    {
        string truePath = Path.Join(_brawlPath, swfPath);
        return AssetLoader.SwfFileCache.Cache.ContainsKey(truePath);
    }

    public void LoadBoneTypes()
    {
        if (IsBoneTypesLoaded()) return;
        throw new Exception();
    }

    public void LoadSwf(string swfPath)
    {
        AssetLoader.LoadSwf(swfPath);
    }

    public bool TryGetAnmClass(string classIdentifier, [NotNullWhen(true)] out IAnmClass? anmClass)
    {
        if (AssetLoader.AnmClasses.TryGetValue(classIdentifier, out AnmClass? @class))
        {
            anmClass = new AnmClassAdapter(@class);
            return true;
        }
        anmClass = null;
        return false;
    }

    public bool TryGetBoneName(short boneId, [NotNullWhen(true)] out string? boneName)
    {
        boneId--;
        if (_boneTypes is null || boneId < 0 || boneId >= _boneTypes.Length)
        {
            boneName = null;
            return false;
        }
        boneName = _boneTypes[boneId];
        return true;
    }

    public bool TryGetScriptAVar(string swfPath, string spriteName, [NotNullWhen(true)] out uint[]? a)
    {
        a = null;
        return false;
    }

    public bool TryGetSymbolId(string swfPath, string symbolName, out ushort symbolId)
    {
        SwfFileData? swf = AssetLoader.LoadSwf(swfPath);
        if (swf is null)
        {
            symbolId = default;
            return false;
        }

        if (swf.SymbolClass.TryGetValue(symbolName, out symbolId))
        {
            return true;
        }

        symbolId = default;
        return false;
    }

    public bool TryGetTag(string swfPath, ushort tagId, [NotNullWhen(true)] out SwfTagBase? tag)
    {
        SwfFileData? swf = AssetLoader.LoadSwf(swfPath);
        if (swf is null)
        {
            tag = null;
            return false;
        }

        if (swf.SpriteTags.TryGetValue(tagId, out DefineSpriteTag? sprite))
        {
            tag = sprite;
            return true;
        }

        if (swf.ShapeTags.TryGetValue(tagId, out ShapeBaseTag? shape))
        {
            tag = shape;
            return true;
        }

        tag = null;
        return false;
    }
}