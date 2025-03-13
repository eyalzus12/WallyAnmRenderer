using System;
using System.IO;
using System.Xml.Linq;

namespace WallyAnmRenderer;

public class PathPreferences
{
    public const string APPDATA_DIR_NAME = "WallyAnmRenderer";
    public const string FILE_NAME = "PathPreferences.xml";

    public event EventHandler<string>? BrawlhallaPathChanged;
    public event EventHandler<uint>? DecryptionKeyChanged;

    private string? _brawlhallaPath;
    public string? BrawlhallaPath { get => _brawlhallaPath; set => SetBrawlhallaPath(value); }
    public string? BrawlhallaAirPath { get; set; }

    private uint? _decryptionKey;
    public uint? DecryptionKey
    {
        get => _decryptionKey;
        set
        {
            _decryptionKey = value;
            if (value is not null)
                DecryptionKeyChanged?.Invoke(this, value.Value);
        }
    }

    public string? ExportPath { get; set; }

    public static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        APPDATA_DIR_NAME,
        FILE_NAME
    );

    public static PathPreferences Load()
    {
        string? dir = Path.GetDirectoryName(FilePath);
        if (dir is not null)
        {
            Directory.CreateDirectory(dir);
            if (File.Exists(FilePath))
            {
                XElement element;
                using (FileStream file = File.OpenRead(FilePath))
                    element = XElement.Load(file);
                PathPreferences p = new();
                p.Deserialize(element);
                return p;
            }
        }

        return new();
    }

    public void Save()
    {
        XElement e = new(nameof(PathPreferences));
        Serialize(e);
        e.Save(FilePath);
    }

    public PathPreferences() { }

    public void Deserialize(XElement e)
    {
        _brawlhallaPath = e.Element(nameof(BrawlhallaPath))?.Value; // don't trigger setter!
        BrawlhallaAirPath = e.Element(nameof(BrawlhallaAirPath))?.Value;

        string? decryptionKeyString = e.Element(nameof(DecryptionKey))?.Value;
        if (decryptionKeyString is not null && uint.TryParse(decryptionKeyString, out uint key))
            _decryptionKey = key;

        ExportPath = e.Element(nameof(ExportPath))?.Value;
    }

    public void Serialize(XElement e)
    {
        if (BrawlhallaPath is not null)
            e.Add(new XElement(nameof(BrawlhallaPath), BrawlhallaPath));
        if (BrawlhallaAirPath is not null)
            e.Add(new XElement(nameof(BrawlhallaAirPath), BrawlhallaAirPath));
        if (DecryptionKey is not null)
            e.Add(new XElement(nameof(DecryptionKey), DecryptionKey));
        if (ExportPath is not null)
            e.Add(new XElement(nameof(ExportPath)), ExportPath);
    }

    public void SetBrawlhallaPath(string? path)
    {
        if (path is null)
        {
            _brawlhallaPath = null;
            return;
        }

        _brawlhallaPath = path;
        BrawlhallaAirPath ??= Path.Combine(_brawlhallaPath, "BrawlhallaAir.swf");
        BrawlhallaPathChanged?.Invoke(this, _brawlhallaPath);
    }
}