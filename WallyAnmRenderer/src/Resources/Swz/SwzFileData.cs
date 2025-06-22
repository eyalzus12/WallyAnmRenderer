using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using BrawlhallaSwz;
using nietras.SeparatedValues;

namespace WallyAnmRenderer;

public sealed class SwzFileData
{
    private readonly Dictionary<string, string> _files = [];

    private SwzFileData() { }
    private SwzFileData(Dictionary<string, string> files) { _files = files; }

    public static SwzFileData New(string filePath, uint key, HashSet<string> filesToRead)
    {
        Dictionary<string, string> files = [];
        using FileStream stream = File.OpenRead(filePath);
        using SwzReader reader = new(stream, key);
        foreach (string file in reader.ReadFiles())
        {
            string filename = SwzUtils.GetFileName(file);
            if (!filesToRead.Contains(filename)) continue;
            files[filename] = file;
        }
        return new(files);
    }

    public static async Task<SwzFileData> NewAsync(string filePath, uint key, HashSet<string> filesToRead, CancellationToken cancellationToken = default)
    {
        Dictionary<string, string> files = [];
        using FileStream stream = FileUtils.OpenReadAsyncSeq(filePath);
        using SwzReader reader = new(stream, key);
        await foreach (string file in reader.ReadFilesAsync(cancellationToken))
        {
            string filename = SwzUtils.GetFileName(file);
            if (!filesToRead.Contains(filename)) continue;
            files[filename] = file;
        }
        return new(files);
    }

    public string this[string filename] => _files[filename];

    public bool TryGetFile(string filename, [MaybeNullWhen(false)] out string file)
    {
        return _files.TryGetValue(filename, out file);
    }
}

public sealed class SwzInitFile
{
    private const string BONE_TYPES = "BoneTypes.xml";
    private const string BONE_SOURCES = "BoneSources.xml";
    private static readonly HashSet<string> TO_READ = [BONE_TYPES, BONE_SOURCES];

    public BoneTypes BoneTypes { get; }
    public BoneSources BoneSources { get; }

    private SwzInitFile(SwzFileData data)
    {
        string boneTypesContent = data[BONE_TYPES];
        XElement boneTypesElement = XElement.Parse(boneTypesContent);
        BoneTypes = new(boneTypesElement);

        string boneSourcesContent = data[BONE_SOURCES];
        XElement boneSourcesElement = XElement.Parse(boneSourcesContent);
        BoneSources = new(boneSourcesElement);
    }

    public static SwzInitFile New(string filePath, uint key)
    {
        return new(SwzFileData.New(filePath, key, TO_READ));
    }

    public static async Task<SwzInitFile> NewAsync(string filePath, uint key, CancellationToken cancellationToken = default)
    {
        return new(await SwzFileData.NewAsync(filePath, key, TO_READ, cancellationToken));
    }
}

public sealed class SwzGameFile
{
    private const string COSTUME_TYPES = "costumeTypes.csv";
    private const string WEAPON_SKIN_TYPES = "weaponSkinTypes.csv";
    private const string ITEM_TYPES = "itemTypes.csv";
    private const string SPAWN_BOT_TYPES = "SpawnBotTypes.xml";
    private const string COMPANION_TYPES = "CompanionTypes.xml";
    private const string COLOR_SCHEME_TYPES = "ColorSchemeTypes.xml";
    private const string COLOR_EXCEPTION_TYPES = "colorExceptionTypes.csv";
    private const string HERO_TYPES = "HeroTypes.xml";
    private static readonly HashSet<string> TO_READ = [
        COSTUME_TYPES,
        WEAPON_SKIN_TYPES,
        ITEM_TYPES,
        SPAWN_BOT_TYPES,
        COMPANION_TYPES,
        COLOR_SCHEME_TYPES,
        COLOR_EXCEPTION_TYPES,
        HERO_TYPES
    ];

    public CostumeTypes CostumeTypes { get; }
    public WeaponSkinTypes WeaponSkinTypes { get; }
    public ItemTypes ItemTypes { get; }
    public SpawnBotTypes SpawnBotTypes { get; }
    public CompanionTypes CompanionTypes { get; }
    public ColorSchemeTypes ColorSchemeTypes { get; }
    public ColorExceptionTypes ColorExceptionTypes { get; }
    public HeroTypes HeroTypes { get; }

    private SwzGameFile(SwzFileData data)
    {
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

        string heroTypesContent = data[HERO_TYPES];
        XElement heroTypesElement = XElement.Parse(heroTypesContent);
        HeroTypes = new(heroTypesElement);

        string costumeTypesContent = data[COSTUME_TYPES];
        using (SepReader reader = readerFromText(costumeTypesContent))
            CostumeTypes = new(reader, HeroTypes);

        string weaponSkinTypesContent = data[WEAPON_SKIN_TYPES];
        using (SepReader reader = readerFromText(weaponSkinTypesContent))
            WeaponSkinTypes = new(reader, CostumeTypes);

        string itemTypesContent = data[ITEM_TYPES];
        using (SepReader reader = readerFromText(itemTypesContent))
            ItemTypes = new(reader);

        string spawnBotTypesContent = data[SPAWN_BOT_TYPES];
        XElement spawnBotTypesElement = XElement.Parse(spawnBotTypesContent);
        SpawnBotTypes = new(spawnBotTypesElement);

        string companionTypesContent = data[COMPANION_TYPES];
        XElement companionTypesElement = XElement.Parse(companionTypesContent);
        CompanionTypes = new(companionTypesElement);

        string colorSchemeTypesContent = data[COLOR_SCHEME_TYPES];
        XElement colorSchemeElement = XElement.Parse(colorSchemeTypesContent);
        ColorSchemeTypes = new(colorSchemeElement);

        string colorExceptionTypesContent = data[COLOR_EXCEPTION_TYPES];
        using (SepReader reader = readerFromText(colorExceptionTypesContent))
            ColorExceptionTypes = new(reader);
    }

    public static SwzGameFile New(string filePath, uint key)
    {
        return new(SwzFileData.New(filePath, key, TO_READ));
    }

    public static async Task<SwzGameFile> NewAsync(string filePath, uint key, CancellationToken cancellationToken = default)
    {
        return new(await SwzFileData.NewAsync(filePath, key, TO_READ, cancellationToken));
    }
}