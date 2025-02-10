using System.Collections.Generic;
using System.IO;
using BrawlhallaSwz;

namespace WallyAnmRenderer;

public static class SwzUtils
{
    public static Dictionary<string, string> GetFilesFromSwz(string swzPath, uint key, string[] fileNames)
    {
        HashSet<string> files = [.. fileNames];
        Dictionary<string, string> result = [];

        using FileStream file = File.OpenRead(swzPath);
        using SwzReader reader = new(file, key);
        while (reader.HasNext())
        {
            string swzFile = reader.ReadFile();
            string fileName = BrawlhallaSwz.SwzUtils.GetFileName(swzFile);
            if (files.Contains(fileName))
            {
                result[fileName] = swzFile;
            }
        }

        return result;
    }

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