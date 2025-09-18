using System.IO;
using System.Threading.Tasks;
using SwfLib;

namespace WallyAnmRenderer;

public readonly record struct SwfOverride(string OriginalPath, string OverridePath, SwfFileData Data)
{
    public static Task<SwfOverride> Create(string originalPath, string overridePath)
    {
        return Task.Run(() =>
        {
            SwfFile swfFile;
            using (FileStream file = File.OpenRead(overridePath))
                swfFile = SwfFile.ReadFrom(file);
            SwfFileData data = SwfFileData.CreateFrom(swfFile);
            return new SwfOverride(originalPath, overridePath, data);
        });
    }
}