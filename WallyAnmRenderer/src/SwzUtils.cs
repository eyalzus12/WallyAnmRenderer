using System.IO;
using BrawlhallaSwz;

namespace WallyAnmRenderer;

public static class SwzUtils
{
    public static string? GetFileFromSwz(string swzPath, uint key, string fileName)
    {
        using FileStream file = File.OpenRead(swzPath);
        using SwzReader reader = new(file, key);
        while (reader.HasNext())
        {
            string swzFile = reader.ReadFile();
            if (BrawlhallaSwz.SwzUtils.GetFileName(swzFile) == fileName)
            {
                return swzFile;
            }
        }
        return null;
    }
}