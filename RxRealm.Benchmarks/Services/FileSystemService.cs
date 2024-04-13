using RxRealm.Core.Services;

namespace RxRealm.Benchmarks.Services;

public class FileSystemService : IFileSystemService
{
    public FileSystemService()
    {
        if (!Directory.Exists(AppDataFolderPath))
        {
            Directory.CreateDirectory(AppDataFolderPath);
        }
    }

    public string AppDataFolderPath => Path.Combine(Environment.CurrentDirectory);

    public Task<Stream> OpenAppPackageFileAsync(string name, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<Stream>(File.OpenRead(Path.Combine(AppDataFolderPath, name)));
    }
}