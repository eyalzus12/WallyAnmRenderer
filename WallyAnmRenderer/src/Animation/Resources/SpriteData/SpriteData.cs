using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using BrawlhallaAnimLib.Gfx;
using nietras.SeparatedValues;

namespace WallyAnmRenderer;

public sealed class SpriteData
{
    private readonly Dictionary<(string, string), SpriteDataInfo> _spriteData = [];
    public SpriteData() { }

    public void ApplySpriteData(SepReader reader)
    {
        foreach (SepReader.Row row in reader)
        {
            string setName = row["SetName"].ToString();
            string boneName = row["BoneName"].ToString();
            string file = row["File"].ToString();
            double width = row["Width"].Parse<double>();
            double height = row["Height"].Parse<double>();
            double offsetX = row["xOffset"].Parse<double>();
            double offsetY = row["yOffset"].Parse<double>();

            if (setName.Length == 0)
            {
                offsetX = 0;
                offsetY = 0;
                width = 128;
                height = 128;
            }

            _spriteData[(setName, boneName)] = new()
            {
                SetName = setName,
                BoneName = boneName,
                File = file,
                Width = width,
                Height = height,
                XOffset = offsetX,
                YOffset = offsetY
            };
        }
    }

    public bool TryGetSpriteData(string setName, string boneName, [MaybeNullWhen(false)] out SpriteDataInfo? spriteData)
    {
        return _spriteData.TryGetValue((setName, boneName), out spriteData);
    }
}

public sealed class SpriteDataInfo : ISpriteData
{
    public required string SetName { get; init; }
    public required string BoneName { get; init; }
    public required string File { get; init; }
    public required double Width { get; init; }
    public required double Height { get; init; }
    public required double XOffset { get; init; }
    public required double YOffset { get; init; }
}