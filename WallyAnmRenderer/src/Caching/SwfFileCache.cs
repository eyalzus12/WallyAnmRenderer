using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SwfLib;

namespace WallyAnmRenderer;

public sealed class SwfFileCache : ManagedCache<string, SwfFileData>
{
    protected override Task<SwfFileData> LoadInternal(string path, CancellationToken ctoken)
    {
        return Task.Run(() =>
        {
            ctoken.ThrowIfCancellationRequested();

            SwfFile swfFile;
            using (FileStream file = File.OpenRead(path))
                swfFile = SwfFile.ReadFrom(file);

            ctoken.ThrowIfCancellationRequested();

            SwfFileData swf = SwfFileData.CreateFrom(swfFile, ctoken);

            return swf;
        }, ctoken);
    }
}