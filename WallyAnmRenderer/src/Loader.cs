using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using BrawlhallaAnimLib;
using BrawlhallaAnimLib.Anm;
using BrawlhallaSwz;
using SwfLib;
using SwfLib.Data;
using SwfLib.Tags;
using SwfLib.Tags.ControlTags;
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
    }

    private readonly Dictionary<string, AnmFile> _anms = [];
    private readonly string[]? _boneTypes = null;
    private readonly Dictionary<string, SwfFile> _swfs = [];

    public bool IsAnmLoaded(string anmPath) => _anms.ContainsKey(anmPath);
    public bool IsBoneTypesLoaded() => _boneTypes is not null;
    public bool IsSwfLoaded(string swfPath) => _swfs.ContainsKey(swfPath);

    public void LoadAnm(string anmPath)
    {
        if (IsAnmLoaded(anmPath)) return;
        string path = Path.Join(_brawlPath, anmPath);
        using FileStream file = File.OpenRead(path);
        _anms[anmPath] = AnmFile.CreateFrom(file);
    }

    public void LoadBoneTypes()
    {
        if (IsBoneTypesLoaded()) return;
        throw new Exception();
    }

    public void LoadSwf(string swfPath)
    {
        if (IsSwfLoaded(swfPath)) return;
        string path = Path.Join(_brawlPath, swfPath);
        using FileStream file = File.OpenRead(path);
        _swfs[swfPath] = SwfFile.ReadFrom(file);
    }

    public bool TryGetAnmClass(string classIdentifier, [NotNullWhen(true)] out IAnmClass? anmClass)
    {
        foreach (AnmFile anm in _anms.Values)
        {
            if (anm.Classes.TryGetValue(classIdentifier, out AnmClass? @class))
            {
                anmClass = new AnmClassAdapter(@class);
                return true;
            }
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
        if (!_swfs.TryGetValue(swfPath, out SwfFile? swf))
        {
            symbolId = default;
            return false;
        }

        SymbolClassTag symbolClass = swf.Tags.OfType<SymbolClassTag>().First();
        foreach (SwfSymbolReference symbolRef in symbolClass.References)
        {
            if (symbolRef.SymbolName == symbolName)
            {
                symbolId = symbolRef.SymbolID;
                return true;
            }
        }

        symbolId = default;
        return false;
    }

    public bool TryGetTag(string swfPath, ushort tagId, [NotNullWhen(true)] out SwfTagBase? tag)
    {
        if (!_swfs.TryGetValue(swfPath, out SwfFile? swf))
        {
            tag = null;
            return false;
        }

        foreach (SwfTagBase swfTag in swf.Tags)
        {
            if (swfTag is DefineSpriteTag defineSprite && defineSprite.SpriteID == tagId)
            {
                tag = defineSprite;
                return true;
            }
            else if (swfTag is ShapeBaseTag defineShape && defineShape.ShapeID == tagId)
            {
                tag = defineShape;
                return true;
            }
        }

        tag = null;
        return false;
    }
}