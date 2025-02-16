using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Linq;

namespace WallyAnmRenderer;

public sealed class BoneSources
{
    private readonly Dictionary<string, string> _boneSources = [];

    public BoneSources(XElement element)
    {
        foreach (XElement original in element.Elements("Original"))
        {
            XElement target = original.Element("Target")!;
            string targetName = target.Attribute("name")!.Value;
            foreach (XElement bone in target.Elements("Bone"))
            {
                _boneSources[bone.Value] = "bones/" + targetName;
            }
        }
    }

    public string this[string boneName] => _boneSources[boneName];

    public bool TryGetBoneFilePath(string boneName, [MaybeNullWhen(false)] out string bonePath)
    {
        return _boneSources.TryGetValue(boneName, out bonePath);
    }
}