using System.IO;

namespace WallyAnmRenderer;

public static class StringUtils
{
    public static string EnsurePathSeparatorEnd(string path)
    {
        char sepChar = Path.DirectorySeparatorChar;
        if (path == "") return sepChar.ToString(); // prevent 0 length issues
        char altChar = Path.AltDirectorySeparatorChar;
        if (path[^1] != sepChar && path[^1] != altChar)
        {
            return path + sepChar;
        }
        return path;
    }
}