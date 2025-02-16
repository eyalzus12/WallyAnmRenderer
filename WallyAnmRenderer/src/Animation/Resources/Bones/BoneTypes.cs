using System.Collections.Generic;
using System.Xml.Linq;

namespace WallyAnmRenderer;

public sealed class BoneTypes
{
    private readonly string[] _boneTypes = [];

    public BoneTypes(XElement element)
    {
        List<string> boneTypes = [];
        foreach (XElement bone in element.Elements("Bone"))
        {
            string boneName = bone.Value;
            boneTypes.Add(boneName);
        }
        _boneTypes = [.. boneTypes];
    }

    public int BoneCount => _boneTypes.Length;
    public string this[int index] => _boneTypes[index];
}