namespace RxRealm.Core.Services;

public interface IFileSystemService
{
    public string AppDataFolderPath { get; }
    public Task<Stream> OpenAppPackageFileAsync(string name, CancellationToken cancellationToken = default);
}