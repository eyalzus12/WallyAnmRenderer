using System.Collections.Generic;
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
        Dictionary<string, string> initFiles = SwzUtils.GetFilesFromSwz(init, key, ["BoneTypes.xml", "BoneSources.xml"]);
        string boneTypes = initFiles["BoneTypes.xml"];
        _boneTypes = [.. XElement.Parse(boneTypes).Elements("Bone").Select((ee) => ee.Value)];
        string boneSources = initFiles["BoneSources.xml"];
        XElement boneSourcesElement = XElement.Parse(boneSources);

        _boneSources = [];
        foreach (XElement original in boneSourcesElement.Elements("Original"))
        {
            XElement target = original.Element("Target")!;
            string targetName = target.Attribute("name")!.Value;
            foreach (XElement bone in target.Elements("Bone"))
            {
                _boneSources[bone.Value] = "bones/" + targetName;
            }
        }

        AssetLoader = new(brawlPath);
    }

    private readonly string[] _boneTypes;
    private readonly Dictionary<string, string> _boneSources;
    public AssetLoader AssetLoader { get; }

    public bool LoadBoneTypes()
    {
        return true;
    }

    public bool LoadBoneSources()
    {
        return true;
    }

    public bool SwfExists(string swfPath)
    {
        return File.Exists(Path.Join(_brawlPath, swfPath));
    }

    public bool LoadSwf(string swfPath)
    {
        return AssetLoader.LoadSwf(swfPath) is not null;
    }

    public bool TryGetAnmClass(string classIdentifier, [MaybeNullWhen(false)] out IAnmClass anmClass)
    {
        if (AssetLoader.AnmClasses.TryGetValue(classIdentifier, out AnmClass? @class))
        {
            anmClass = new AnmClassAdapter(@class);
            return true;
        }
        anmClass = null;
        return false;
    }

    public bool TryGetBoneName(short boneId, [MaybeNullWhen(false)] out string boneName)
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

    public bool TryGetBoneFilePath(string boneName, [MaybeNullWhen(false)] out string bonePath)
    {
        return _boneSources.TryGetValue(boneName, out bonePath);
    }

    public bool TryGetScriptAVar(string swfPath, string spriteName, [MaybeNullWhen(false)] out uint[] a)
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

    public bool TryGetTag(string swfPath, ushort tagId, [MaybeNullWhen(false)] out SwfTagBase tag)
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