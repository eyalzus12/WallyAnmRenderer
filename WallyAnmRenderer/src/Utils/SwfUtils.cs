using System.IO;
using SwfLib;
using SwfLib.Tags.ShapeTags;

namespace WallyAnmRenderer;

public static class SwfUtils
{
    public static ShapeBaseTag DeepCloneShape(ShapeBaseTag shape)
    {
        // hacky. creates a byte-level clone of the tag.

        // write the tag data into the stream
        // this is a streaming version of SwfTagSerializer.GetTagData
        SwfTagSerializer serializer = new(null);
        using MemoryStream ms = new();
        SwfStreamWriter writer = new(ms);
        shape.AcceptVistor(serializer, writer);
        writer.FlushBits();
        if (shape.RestData is not null && shape.RestData.Length != 0)
            writer.WriteBytes(shape.RestData);
        writer.Flush();
        ms.Position = 0;
        // read the tag data
        SwfTagDeserializer deserializer = new(null);
        SwfStreamReader reader = new(ms);
        ShapeBaseTag newShape = (ShapeBaseTag)deserializer.ReadTag(shape.TagType, reader);
        return newShape;
    }
}