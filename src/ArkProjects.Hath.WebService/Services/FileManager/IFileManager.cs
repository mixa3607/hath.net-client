using ArkProjects.Hath.WebService.Misc;

namespace ArkProjects.Hath.WebService.Services;

public interface IFileManager : IDisposable
{
    Task InitAsync(CancellationToken ct = default);

    long CacheDiskLimitBytes { get; }
    long CacheSizeBytes { get; }
    long CacheFreeSizeBytes { get; }
    long CacheFilesCount { get; }
    long CacheDriveFreeSpaceBytes { get; }
    double CacheDriveFreeSpaceRatio { get; }

    void UpdateCacheDriveFreeSpace();

    Task ReloadCacheStateAsync(CancellationToken ct = default);
    Task ValidateCacheAsync(CancellationToken ct = default);

    Task<bool> IsFilePresentedAsync(RequestedFile requestedFile, CancellationToken ct = default);
    Task<IHathFileResult> GetPhysicalFileAsync(RequestedFile requestedFile, CancellationToken ct = default);
    Task<IHathFileResult> GetProxiedFileAsync(RequestedFile requestedFile, CancellationToken ct = default);
    Task SaveFileAsync(RequestedFile requestedFile, byte[] fileBytes);

    Task<TempDirectory> GetTempDirectory();

    Task<bool> ValidateFileHashAsync(string filePath, byte[] expectedHash, CancellationToken ct = default);
}