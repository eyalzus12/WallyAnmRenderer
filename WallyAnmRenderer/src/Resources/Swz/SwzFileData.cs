using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Xml.Linq;
using BrawlhallaSwz;
using nietras.SeparatedValues;

namespace WallyAnmRenderer;

public sealed class SwzFileData
{
    private readonly Dictionary<string, string> _files = [];

    public SwzFileData(string filePath, uint key, HashSet<string> filesToRead)
    {
        using FileStream stream = File.OpenRead(filePath);
        using SwzReader reader = new(stream, key);
        while (reader.HasNext())
        {
            string file = reader.ReadFile();
            string filename = SwzUtils.GetFileName(file);
            if (!filesToRead.Contains(filename)) continue;
            _files[filename] = file;
        }
    }

    public string this[string filename] => _files[filename];

    public bool TryGetFile(string filename, [MaybeNullWhen(false)] out string file)
    {
        return _files.TryGetValue(filename, out file);
    }
}

public sealed class SwzInitFile
{
    private readonly SwzFileData _data;

    public BoneTypes BoneTypes { get; }
    public BoneSources BoneSources { get; }

    public SwzInitFile(string filePath, uint key)
    {
        _data = new(filePath, key, ["BoneTypes.xml", "BoneSources.xml"]);

        string boneTypesContent = _data["BoneTypes.xml"];
        XElement boneTypesElement = XElement.Parse(boneTypesContent);
        BoneTypes = new(boneTypesElement);

        string boneSourcesContent = _data["BoneSources.xml"];
        XElement boneSourcesElement = XElement.Parse(boneSourcesContent);
        BoneSources = new(boneSourcesElement);
    }
}

public sealed class SwzGameFile
{
    private readonly SwzFileData _data;

    public CostumeTypes CostumeTypes { get; }
    public WeaponSkinTypes WeaponSkinTypes { get; }
    public ColorSchemeTypes ColorSchemeTypes { get; }
    public ColorExceptionTypes ColorExceptionTypes { get; }
    public HeroTypes HeroTypes { get; }

    public SwzGameFile(string filePath, uint key)
    {
        _data = new(filePath, key, ["costumeTypes.csv", "weaponSkinTypes.csv", "ColorSchemeTypes.xml", "colorExceptionTypes.csv", "HeroTypes.xml"]);

        static SepReader readerFromText(string text)
        {
            StringReader textReader = new(text);
            textReader.ReadLine(); // skip first line bullshit
            SepReaderOptions reader = Sep.New(',').Reader((opts) =>
            {
                return opts with
                {
                    DisableColCountCheck = true,
                    Unescape = true,
                };
            });
            return reader.From(textReader);
        }

        string heroTypesContent = _data["HeroTypes.xml"];
        XElement heroTypesElement = XElement.Parse(heroTypesContent);
        HeroTypes = new(heroTypesElement);

        string costumeTypesContent = _data["costumeTypes.csv"];
        using (SepReader reader = readerFromText(costumeTypesContent))
            CostumeTypes = new(reader, HeroTypes);

        string weaponSkinTypesContent = _data["weaponSkinTypes.csv"];
        using (SepReader reader = readerFromText(weaponSkinTypesContent))
            WeaponSkinTypes = new(reader, CostumeTypes);

        string colorSchemeTypesContent = _data["ColorSchemeTypes.xml"];
        XElement colorSchemeElement = XElement.Parse(colorSchemeTypesContent);
        ColorSchemeTypes = new(colorSchemeElement);

        string colorExceptionTypesContent = _data["colorExceptionTypes.csv"];
        using (SepReader reader = readerFromText(colorExceptionTypesContent))
            ColorExceptionTypes = new(reader);
    }
}