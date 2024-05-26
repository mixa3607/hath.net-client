using ArkProjects.Hath.ClientApi;
using ArkProjects.Hath.ClientApi.Extensions;
using ArkProjects.Hath.ClientApi.Responses;
using ArkProjects.Hath.WebService.Options;
using ArkProjects.Hath.WebService.Services;
using Flurl.Http;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Quartz;

namespace ArkProjects.Hath.WebService.Quartz.Jobs;

//TODO: burn it with fire
public class DownloadPendingGalleriesJob : IJob
{
    private readonly ILogger<DownloadPendingGalleriesJob> _logger;
    private readonly HathClient _client;
    private readonly IFileManager _fileManager;
    private readonly IFilesDownloadHelper _downloadHelper;
    private readonly GalleryDownloaderOptions _options;

    public DownloadPendingGalleriesJob(ILogger<DownloadPendingGalleriesJob> logger, HathClient client,
        IFileManager fileManager, IFilesDownloadHelper downloadHelper, IOptions<GalleryDownloaderOptions> options)
    {
        _logger = logger;
        _client = client;
        _fileManager = fileManager;
        _downloadHelper = downloadHelper;
        _options = options.Value;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var ct = context.CancellationToken;
        int? prevGid = null;
        string? prevXRes = null;
        while (true)
        {
            try
            {
                var gallery = await _client.GetDownloadQueueAsync(prevGid, prevXRes, ct);
                await DownloadGalleryAsync(gallery.Gallery!, ct);
                prevGid = gallery.Gallery!.GalleryId;
                prevXRes = gallery.Gallery!.MinXRes;
            }
            catch (Exception e) when ((e as HathClientException)?.Response.Status is "NO_PENDING_DOWNLOADS"
                                      or "EMPTY_RESPONSE")
            {
                _logger.LogInformation("All galleries downloaded");
                break;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Gallery downloading failed with error");
                break;
            }
        }
    }


    private async Task DownloadGalleryAsync(HathGalleryInfo galleryInfo, CancellationToken ct = default)
    {
        using var activity = Telemetry.ActivitySource.StartActivity();
        activity?.AddTag("GID", galleryInfo.GalleryId);

        var galleryPathPersist =
            Path.Combine(_options.DownloadsStoragePath, $"{galleryInfo.GalleryId}_{galleryInfo.MinXRes}");
        if (Directory.Exists(galleryPathPersist))
        {
            _logger.LogWarning("Gallery {gid} with res {res} already exist. Skip", 
                galleryInfo.GalleryId, galleryInfo.MinXRes);
            return;
        }

        _logger.LogInformation("Begin download gallery {name} ({gid})", galleryInfo.Title, galleryInfo.GalleryId);
        using var galleryPathTmp = await _fileManager.GetTempDirectory();
        galleryPathTmp.Create();

        await SaveGalleryInfoJsonAsync(galleryPathTmp.Path, galleryInfo);
        await SaveGalleryInfoTextAsync(galleryPathTmp.Path, galleryInfo);
        var failures = new List<FileDownloadFailure>();
        await Parallel.ForEachAsync(galleryInfo.Pages.Select((x, i) => i), new ParallelOptions()
        {
            CancellationToken = ct,
            MaxDegreeOfParallelism = _options.MaxParallelFilesDownload,
        }, async (i, _) =>
        {
            var err = await DownloadPageAsync(galleryPathTmp.Path, galleryInfo, i, ct);
            if (err == null)
            {
                _logger.LogInformation("File downloaded ({curr}/{total})", i + 1, galleryInfo.Pages.Count);
            }
            else
            {
                failures.Add(err);
            }
        });

        if (failures.Count > 0)
        {
            if (failures.Count < 30)
            {
                try
                {
                    await _client.DownloaderFailuresAsync(failures, ct);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Failure notify server about download errors");
                }
            }

            return;
        }
        
        galleryPathTmp.Move(galleryPathPersist);
    }

    private async Task<FileDownloadFailure?> DownloadPageAsync(string basePath, HathGalleryInfo galleryInfo,
        int pageIdx,
        CancellationToken ct = default)
    {
        using var activity = Telemetry.ActivitySource.StartActivity();
        activity?.AddTag("GID", galleryInfo.GalleryId);
        activity?.AddTag("IDX", pageIdx);

        var maxAttempts = 3;
        var curAttempt = 0;
        var page = galleryInfo.Pages[pageIdx];
        string? host = null;
        while (curAttempt < maxAttempts)
        {
            try
            {
                var dest = Path.Combine(basePath, $"{pageIdx:D3}.{page.Type}");
                var urlsResponse = await _client.DownloaderFetchAsync(galleryInfo.GalleryId, pageIdx + 1,
                    page.FileIndex, page.XRes, curAttempt, ct);
                var downloadUrls = _downloadHelper.MapUrls(urlsResponse.Lines);
                foreach (var downloadUrl in downloadUrls)
                {
                    try
                    {
                        host = downloadUrl.Host;
                        {
                            var request = _downloadHelper.GetRequest(downloadUrl);
                            await using var netStream = await request
                                .GetStreamAsync(HttpCompletionOption.ResponseHeadersRead, ct);
                            await using var fileStream =
                                File.Open(dest, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                            await netStream.CopyToAsync(fileStream, ct);
                        }

                        if (page.Hash != null &&
                            !await _fileManager.ValidateFileHashAsync(dest, page.Hash.HexToBytes(), ct))
                        {
                            throw new Exception("File hash is invalid");
                        }

                        return null;
                    }
                    catch (Exception e)
                    {
                        //ignore
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Problem while downloading page");
                curAttempt++;
            }
        }

        _logger.LogError("Max attempts for download page reached");
        return new FileDownloadFailure()
        {
            FileIndex = page.FileIndex,
            Host = host ?? "",
            XRes = page.XRes
        };
    }


    public async Task<string> SaveGalleryInfoJsonAsync(string galleryPath, HathGalleryInfo gallery)
    {
        var jsonStr = JsonConvert.SerializeObject(gallery, Formatting.Indented);
        var outPath = Path.Combine(galleryPath, "info.json");
        await File.WriteAllTextAsync(outPath, jsonStr);
        return outPath;
    }

    public async Task<string> SaveGalleryInfoTextAsync(string galleryPath, HathGalleryInfo gallery)
    {
        var outPath = Path.Combine(galleryPath, "info.txt");
        await File.WriteAllTextAsync(outPath, gallery.About);
        return outPath;
    }
}