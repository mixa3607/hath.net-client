namespace ArkProjects.Hath.WebService.Services;

public class GalleryDownloaderOptions
{
    public string? CheckPeriod { get; set; }
    public int MaxParallelFilesDownload { get; set; } = 3;
    public required string DownloadsStoragePath { get; set; }
}