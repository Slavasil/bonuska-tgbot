namespace SimpleTGBot;

internal class TempStorage : IDisposable
{
    private string directory;
    private List<string> createdTempFiles;
    private Random rng;

    public TempStorage()
    {
        rng = new Random();

        do
        {
            directory = Path.GetTempPath() + "/demotivatorBot-" + RandomHexString(4);
        } while (Directory.Exists(directory));
        Directory.CreateDirectory(directory);

        createdTempFiles = new List<string>();
    }

    public void Dispose()
    {
        foreach (string filename in createdTempFiles)
        {
            if (File.Exists(filename))
            {
                File.Delete(filename);
            }
        }
        Directory.Delete(directory);
    }

    public (string, FileStream) newTemporaryFile(string? prefix, string? extension)
    {
        string filename = directory + "/" + (prefix != null ? prefix + "-" : "") + RandomHexString(8) + (extension != null ? "." + extension : "");
        FileStream file = File.Open(filename, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
        createdTempFiles.Add(filename);
        return (filename, file);
    }

    public void deleteTemporaryFile(string filename)
    {
        if (createdTempFiles.Contains(filename))
        {
            File.Delete(filename);
            createdTempFiles.Remove(filename);
        }
    }

    private string RandomHexString(int n)
    {
        return rng.NextInt64(1L << (n << 2)).ToString("X" + n);
    }
}
