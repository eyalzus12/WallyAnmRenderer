using System.IO;

namespace WallyAnmRenderer;

public sealed class SwfFileCache : ManagedCache<string, SwfFileData>
{
    protected override SwfFileData LoadInternal(string path)
    {
        using FileStream file = File.OpenRead(path);
        SwfFileData swf = SwfFileData.CreateFrom(file);
        return swf;
    }
}