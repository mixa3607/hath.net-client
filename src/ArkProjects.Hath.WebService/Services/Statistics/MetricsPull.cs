using System.Diagnostics;
using System.Diagnostics.Metrics;
using ArkProjects.Hath.WebService.HathApi;

namespace ArkProjects.Hath.WebService.Services;

public class MetricsPull
{
    private readonly IHathServerLifetimeService _hathServerLifetimeService;
    private readonly ISettingsStorage _settingsStorage;
    private readonly IFileManager _fileManager;
    private readonly HathOverloadDetector _overloadDetector;

    public MetricsPull(IMeterFactory meterFactory, ISettingsStorage settingsStorage,
        IHathServerLifetimeService hathServerLifetimeService, IFileManager fileManager,
        HathOverloadDetector overloadDetector)
    {
        _settingsStorage = settingsStorage;
        _hathServerLifetimeService = hathServerLifetimeService;
        _fileManager = fileManager;
        _overloadDetector = overloadDetector;

        var meter = meterFactory.Create(Telemetry.ServiceName, "0.1");

        meter.CreateObservableGauge("eh_hath_current_version",
            GetCurrentClientBuild, null, "Current version");
        meter.CreateObservableGauge("eh_hath_minimal_version",
            GetMinimalClientBuild, null, "Minimal version");
        meter.CreateObservableGauge("eh_hath_latest_version",
            GetLatestClientBuild, null, "Latest version");

        meter.CreateObservableGauge("eh_hath_client_status",
            GetStatus, null, $"Client Status. Values: {EnumValuesToString<HathServerStatus>()}");

        meter.CreateObservableGauge("eh_hath_client_uptime_seconds",
            GetUptime, null, "Client uptime in seconds. Reset on suspend");
        
        meter.CreateObservableGauge("eh_hath_cache_size_limit_bytes",
            GetMaxCacheSize, null, "Reserved maximal cache size");
        
        meter.CreateObservableGauge("eh_hath_cache_size_bytes",
            GetCacheSize, null, "Cache size");
        
        meter.CreateObservableGauge("eh_hath_cache_size_free_bytes",
            GetCacheFreeSize, null, "Free space reserved for H@H cache");
        
        meter.CreateObservableGauge("eh_hath_cache_file_count",
            GetCacheFilesCount, null, "Count of files currently in client cache");
        
        meter.CreateObservableGauge("eh_hath_overload_notifies_count",
            GetOverloadNotifies, null, "Overload notifies count");
        
        meter.CreateObservableGauge("eh_hath_connections_max_count",
            GetMaxConnections, null, "Max connections");
        
        meter.CreateObservableGauge("eh_hath_last_server_contact_epoch",
            GetLastServerContact, null, "Epoch timestamp of last server contact");

        meter.CreateObservableGauge("eh_hath_cache_drive_free_bytes",
            GetDriveFreeSpaceBytes, null, "Free bytes in cache drive");
        meter.CreateObservableGauge("eh_hath_cache_drive_free_ratio",
            GetDriveFreeSpaceRatio, null, "Free ratio in cache drive");
    }

    public Task InitAsync()
    {
        return Task.CompletedTask;
    }

    private double GetUptime() =>
        _hathServerLifetimeService.ServerStatus == HathServerStatus.Running
            ? (DateTimeOffset.UtcNow - _hathServerLifetimeService.LastStart)?.TotalSeconds ?? 0
            : 0;

    private Measurement<double> GetDriveFreeSpaceRatio() =>
        new((double)_fileManager.CacheDriveFreeSpaceRatio, GetDefaultTagList());

    private Measurement<long> GetDriveFreeSpaceBytes() =>
        new((long)_fileManager.CacheDriveFreeSpaceBytes, GetDefaultTagList());

    private Measurement<long> GetCurrentClientBuild() =>
        new((long)_settingsStorage.Settings.CurrentClientBuild, GetDefaultTagList());

    private Measurement<long> GetLatestClientBuild() =>
        new((long)_settingsStorage.Settings.LatestClientBuild, GetDefaultTagList());

    private Measurement<long> GetMinimalClientBuild() =>
        new((long)_settingsStorage.Settings.MinimalClientBuild, GetDefaultTagList());

    private Measurement<long> GetLastServerContact() =>
        new((long)(_hathServerLifetimeService.LastProlongSession?.ToUnixTimeSeconds() ?? 0), GetDefaultTagList());

    private Measurement<long> GetMaxConnections() =>
        new((long)_overloadDetector.MaxConnections, GetDefaultTagList());

    private Measurement<long> GetOverloadNotifies() =>
        new((long)_overloadDetector.OverloadNotifies, GetDefaultTagList());

    private Measurement<long> GetCacheFilesCount() =>
        new((long)_fileManager.CacheFilesCount, GetDefaultTagList());

    private Measurement<long> GetCacheFreeSize() =>
        new((long)_fileManager.CacheFreeSizeBytes, GetDefaultTagList());

    private Measurement<long> GetCacheSize() =>
        new((long)_fileManager.CacheSizeBytes, GetDefaultTagList());

    private Measurement<long> GetMaxCacheSize() =>
        new((long)_fileManager.CacheDiskLimitBytes, GetDefaultTagList());

    private Measurement<long> GetStatus() =>
        new((long)_hathServerLifetimeService.ServerStatus, GetDefaultTagList());

    private string EnumValuesToString<TEnum>() where TEnum : struct, Enum
    {
        return string.Join(", ", Enum.GetValues<TEnum>().Select(x => $"{x} = {(int)(object)x}"));
    }

    private TagList GetDefaultTagList()
    {
        return new TagList();
    }
}