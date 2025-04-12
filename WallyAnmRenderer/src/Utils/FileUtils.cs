using System.IO;

namespace WallyAnmRenderer;

public static class FileUtils
{
    public static FileStream OpenReadAsyncSeq(string path)
    {
        return new(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
    }

    public static FileStream CreateWriteAsync(string path)
    {
        return new(path, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous);
    }
}