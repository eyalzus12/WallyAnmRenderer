using System.IO;

namespace WallyAnmRenderer;

public class SwfFileCache : ManagedCache<string, SwfFileData>
{
    protected override SwfFileData LoadInternal(string path)
    {
        path = Path.GetFullPath(path);

        SwfFileData swf;
        using (FileStream stream = File.OpenRead(path))
            swf = SwfFileData.CreateFrom(stream);
        return swf;
    }
}