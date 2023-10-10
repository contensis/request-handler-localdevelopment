using Zengenti.Contensis.RequestHandler.LocalDevelopment.Models;

namespace Zengenti.Contensis.RequestHandler.LocalDevelopment;

public class SiteConfigLoader : IDisposable
{
    private readonly FileSystemWatcher? _fileSystemWatcher;

    public SiteConfig SiteConfig { get; private set; }

    public SiteConfigLoader(string configFilePath)
    {
        if (!File.Exists(configFilePath))
        {
            throw new FileNotFoundException("A valid site config file is required", configFilePath);
        }

        SiteConfig = SiteConfig.LoadFromFile(configFilePath)!;

        var directory = Path.GetDirectoryName(configFilePath)!;
        var filename = Path.GetFileName(configFilePath);

        _fileSystemWatcher = new FileSystemWatcher(directory, filename);
        _fileSystemWatcher.Changed += FileSystemWatcher_Changed;
        _fileSystemWatcher.EnableRaisingEvents = true;
    }

    public SiteConfigLoader(string alias, string projectId, string accessToken, string clientId, string sharedSecret,
        string blocksAsJson, string? renderersAsJason = null)
    {
        SiteConfig = SiteConfig.LoadFromJson(alias, projectId, accessToken, clientId, sharedSecret, blocksAsJson,
            renderersAsJason);
    }

    private void FileSystemWatcher_Changed(object sender, FileSystemEventArgs e)
    {
        SiteConfig = SiteConfig.LoadFromFile(e.FullPath)!;
    }

    #region IDisposable Support

    private bool _disposedValue; // To detect redundant calls

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing && _fileSystemWatcher != null)
            {
                _fileSystemWatcher.Dispose();
            }

            SiteConfig = null!;
            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
    }

    #endregion
}