using System.Security.Cryptography;
using ArkProjects.Hath.ClientApi.Extensions;
using ArkProjects.Hath.WebService.Misc;
using Microsoft.Extensions.Options;

namespace ArkProjects.Hath.WebService.Services;

public class FileManager : IFileManager
{
    public long CacheDiskLimitBytes => _settingsStorage.Settings.DiskLimitBytes;
    public long CacheSizeBytes => _state.CacheSize;
    public long CacheFreeSizeBytes => CacheDiskLimitBytes - CacheSizeBytes;
    public long CacheFilesCount => _state.CacheFilesCount;
    public long CacheDriveFreeSpaceBytes { get; private set; } = -1;
    public double CacheDriveFreeSpaceRatio { get; private set; } = -1;

    private readonly FileManagerOptions _options;
    private readonly ILogger<FileManager> _logger;
    private readonly ISettingsStorage _settingsStorage;
    private FileManagerState _state;

    public FileManager(IOptions<FileManagerOptions> options, ILogger<FileManager> logger,
        ISettingsStorage settingsStorage)
    {
        _logger = logger;
        _settingsStorage = settingsStorage;
        _options = options.Value;
        _state = new FileManagerState();
    }

    public async Task InitAsync(CancellationToken ct = default)
    {
        _options.CacheStoragePath = Path.GetFullPath(_options.CacheStoragePath);
        if (!Directory.Exists(_options.CacheStoragePath))
            Directory.CreateDirectory(_options.CacheStoragePath);

        _options.TempStoragePath = Path.GetFullPath(_options.TempStoragePath);
        if (!Directory.Exists(_options.TempStoragePath))
            Directory.CreateDirectory(_options.TempStoragePath);

        _options.StateFilePath = Path.GetFullPath(_options.StateFilePath);
        _state = FileManagerState.Read(_options.StateFilePath);
        if (_state.CacheSize == 0 || _state.CacheFilesCount == 0)
        {
            await ReloadCacheStateAsync(ct);
            _state.Save(_options.StateFilePath);
        }

        _logger.LogInformation("Now in cache {files} files with total size {size}",
            _state.CacheFilesCount, _state.CacheSize);
    }

    public void UpdateCacheDriveFreeSpace()
    {
        var drive = DriveInfo.GetDrives()
            .Where(x => x.IsReady && _options.CacheStoragePath.StartsWith(x.RootDirectory.FullName))
            .MaxBy(x => x.RootDirectory.FullName.Length);
        if (drive == null)
        {
            CacheDriveFreeSpaceBytes = -1;
            CacheDriveFreeSpaceRatio = -1;
        }
        else
        {
            CacheDriveFreeSpaceBytes = drive.AvailableFreeSpace;
            CacheDriveFreeSpaceRatio = (double)drive.AvailableFreeSpace / drive.TotalSize;
        }
    }

    public Task ReloadCacheStateAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Recalculate cache state");
        _state.CacheFilesCount = 0;
        _state.CacheSize = 0;
        foreach (var path in Directory.EnumerateFiles(_options.CacheStoragePath, "*", SearchOption.AllDirectories))
        {
            _state.AddFile(new FileInfo(path).Length);
        }
        return Task.CompletedTask;
    }


    public async Task ValidateCacheAsync(CancellationToken ct = default)
    {
        var paths = Directory.EnumerateFiles(_options.CacheStoragePath, "*", SearchOption.AllDirectories);
        var ok = 0;
        var fail = 0;
        await Parallel.ForEachAsync(paths, new ParallelOptions()
        {
            CancellationToken = ct,
            MaxDegreeOfParallelism = 50,
        }, async (path, _) =>
        {
            var expectedHash = RequestedFile.Parse(Path.GetFileName(path)).Hash.HexToBytes();
            if (!await ValidateFileHashAsync(path, expectedHash, ct))
            {
                _logger.LogError("SHA1 for file {file} is invalid", path);
                Interlocked.Increment(ref fail);
            }
            else
            {
                Interlocked.Increment(ref ok);
            }

            if ((ok + fail) % 1000 == 0)
            {
                _logger.LogInformation("Validation progress. OK: {ok} / BAD: {bad}", ok, fail);
            }
        });

        _logger.LogInformation("Validation completed. OK: {ok} / BAD: {bad}", ok, fail);
    }

    public async Task<bool> ValidateFileHashAsync(string filePath, byte[] expectedHash, CancellationToken ct = default)
    {
        await using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 0,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        var hash = await SHA1.HashDataAsync(fileStream, ct);
        return MemoryExtensions.SequenceEqual<byte>(hash, expectedHash);
    }

    public Task<bool> IsFilePresentedAsync(RequestedFile requestedFile, CancellationToken ct = default)
    {
        var filePath = GetPhysicalFilePath(requestedFile);
        return Task.FromResult(File.Exists(filePath));
    }

    public Task<IHathFileResult> GetPhysicalFileAsync(RequestedFile requestedFile,
        CancellationToken ct = default)
    {
        var filePath = GetPhysicalFilePath(requestedFile);
        return Task.FromResult<IHathFileResult>(new HathPhysicalFileResult(filePath, requestedFile));
    }

    public Task<IHathFileResult> GetProxiedFileAsync(RequestedFile requestedFile, CancellationToken ct = default)
    {
        return Task.FromResult<IHathFileResult>(new HathProxiedFileResult2(requestedFile));
    }

    public string GetPhysicalFilePath(RequestedFile requestedFile)
    {
        return Path.Combine(_options.CacheStoragePath, requestedFile.GetRelativePath());
    }

    public async Task SaveFileAsync(RequestedFile requestedFile, byte[] fileBytes)
    {
        if (!MemoryExtensions.SequenceEqual<byte>(fileBytes.GetSha1(), requestedFile.Hash.HexToBytes()))
            throw new Exception($"Hash for file {requestedFile.GetFileId()} is invalid!");

        var dstFile = GetPhysicalFilePath(requestedFile);
        var dstDir = Path.GetDirectoryName(dstFile);
        if (!Directory.Exists(dstDir))
            Directory.CreateDirectory(dstDir!);

        _logger.LogDebug("Save to file {dst}", dstFile);

        await using var fileStream = File.OpenWrite(dstFile);
        await fileStream.WriteAsync(fileBytes, 0, fileBytes!.Length);
        _state.AddFile(fileBytes.Length);

        _logger.LogDebug("File {dst} saved", dstFile);
    }

    public Task<TempDirectory> GetTempDirectory()
    {
        return Task.FromResult(new TempDirectory(Path.Combine(_options.TempStoragePath, Guid.NewGuid().ToString())));
    }

    public void Dispose()
    {
        _state.Save(_options.StateFilePath);
    }
}