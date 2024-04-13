using RxRealm.Core.Services;

namespace RxRealm.Services;

public class FileSystemService : IFileSystemService
{
    public string AppDataFolderPath => Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

    public Task<Stream> OpenAppPackageFileAsync(string name, CancellationToken cancellationToken = default)
    {
        return FileSystem.OpenAppPackageFileAsync(name);
    }
}