namespace ArkProjects.Hath.WebService.Services;

public class FileDownloadingOptions
{
    public FilesDownloadingUrlMappingMode UrlMappingMode { get; init; }
    public FilesDownloadingSslCheckMode SslCheckMode { get; init; }
    public bool UseProxy { get; init; }
    public string? ProxyUrl { get; init; }
    public string? ProxyLogin { get; init; }
    public string? ProxyPassword { get; init; }
}