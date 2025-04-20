namespace SimpleTGBot;

internal class TempStorage : IDisposable
{
    private string directory;
    private Dictionary<FileStream, string> createdTempFiles;
    private Random rng;

    public TempStorage()
    {
        rng = new Random();

        do
        {
            directory = Path.GetTempPath() + "/demotivatorBot-" + RandomHexString(4);
        } while (Directory.Exists(directory));
        Directory.CreateDirectory(directory);

        createdTempFiles = new Dictionary<FileStream, string>();
    }

    public void Dispose()
    {
        foreach (var kv in createdTempFiles)
        {
            kv.Key.Dispose();
            if (File.Exists(kv.Value))
            {
                File.Delete(kv.Value);
            }
        }
        Directory.Delete(directory);
    }

    public (string, FileStream) newTemporaryFile(string? prefix, string? extension)
    {
        string filename = directory + "/" + (prefix != null ? prefix + "-" : "") + RandomHexString(8) + (extension != null ? "." + extension : "");
        FileStream file = File.Open(filename, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
        createdTempFiles.Add(file, filename);
        return (filename, file);
    }

    public void deleteTemporaryFile(FileStream file)
    {
        if (createdTempFiles.ContainsKey(file))
        {
            File.Delete(createdTempFiles[file]);
        }
        file.Dispose();
    }

    private string RandomHexString(int n)
    {
        return rng.NextInt64(1L << (n << 2)).ToString("X" + n);
    }
}
