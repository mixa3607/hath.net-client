using System.Buffers;
using System.Collections.Concurrent;
using ArkProjects.Hath.ClientApi;
using ArkProjects.Hath.WebService.Services;
using Flurl.Http;

namespace ArkProjects.Hath.WebService.Misc;

public class HathProxiedFileResult2 : IHathFileResult
{
    private enum Status
    {
        Ok = 0,
        FetchedUrlsFromRpc = 10,
        ErrorOnFetchUrlsFromRpc = 11,
        FetchedFileHeaders = 20,
        ErrorOnFetchFileHeaders = 21,
        FetchedFileBytes = 30,
        ErrorOnFetchFileBytes = 31,
    }

    public RequestedFile RequestedFile { get; }

    private readonly BlockingCollection<(Status status, ReadOnlyMemory<byte>)> _readingQueue =
        new(new ConcurrentQueue<(Status status, ReadOnlyMemory<byte>)>());

    private ILogger<HathProxiedFileResult2> _logger = null!;
    private ISettingsStorage _settingsStorage = null!;
    private IFileManager _fileManager = null!;
    private IFilesDownloadHelper _downloadHelper = null!;
    private IStatisticsManager _statistics = null!;

    public HathProxiedFileResult2(RequestedFile requestedFile)
    {
        RequestedFile = requestedFile;
    }

    public async Task ExecuteAsync(HttpContext ctx)
    {
        _logger = ctx.RequestServices.GetRequiredService<ILogger<HathProxiedFileResult2>>();
        _fileManager = ctx.RequestServices.GetRequiredService<IFileManager>();
        _settingsStorage = ctx.RequestServices.GetRequiredService<ISettingsStorage>();
        _downloadHelper = ctx.RequestServices.GetRequiredService<IFilesDownloadHelper>();
        _statistics = ctx.RequestServices.GetRequiredService<IStatisticsManager>();

        {
            var hathClient = ctx.RequestServices.GetRequiredService<HathClient>();
            _ = ReceiveRemoteFileAsync(hathClient, ctx.RequestAborted);
        }
        var withError = false;
        try
        {
            await HathProxiedFileSendResponse(ctx);
        }
        catch (Exception e)
        {
            withError = true;
            throw;
        }
        finally
        {
            _ = _statistics.FileTxAsync(ctx.Connection.RemoteIpAddress!, ctx.Response.ContentLength ?? 0, RequestedFile, withError);
        }
    }

    private async Task ReceiveRemoteFileAsync(HathClient hathClient, CancellationToken ct = default)
    {
        using var activity = Telemetry.ActivitySource.StartActivity();
        IReadOnlyList<string> urls;
        try
        {
            var resp = await hathClient.StaticRangeFetchAsync(
                RequestedFile.FileIndex, RequestedFile.XResType!, RequestedFile.GetFileId(),
                CancellationToken.None);

            if (resp.Lines.Count == 0)
                throw new Exception("Fetch 0 urls");

            _readingQueue.Add((Status.FetchedUrlsFromRpc, ReadOnlyMemory<byte>.Empty), CancellationToken.None);
            urls = resp.Lines;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Handle exception on url fetch");
            _readingQueue.Add((Status.ErrorOnFetchUrlsFromRpc, ReadOnlyMemory<byte>.Empty), CancellationToken.None);
            _ = _statistics.FileRxAsync(0, RequestedFile, true);
            return;
        }

        byte[] fileBytes;
        IFlurlResponse response;
        try
        {
            var (flurlResponse, contentLength) = await GetFileResponseAsync(urls, ct);
            response = flurlResponse;
            fileBytes = new byte[contentLength];
            activity?.AddTag("Size", fileBytes.Length);
            _readingQueue.Add((Status.FetchedFileHeaders, new ReadOnlyMemory<byte>(fileBytes)),
                CancellationToken.None);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Handle exception on file headers fetch");
            _readingQueue.Add((Status.ErrorOnFetchFileHeaders, ReadOnlyMemory<byte>.Empty), CancellationToken.None);
            _ = _statistics.FileRxAsync(0, RequestedFile, true);
            return;
        }

        try
        {
            _logger!.LogDebug("Begin reading stream");
            var offset = 0;
            var stream = await response.GetStreamAsync();
            do
            {
                var read = await stream.ReadAsync(fileBytes!, offset, fileBytes!.Length - offset,
                    CancellationToken.None);
                _readingQueue.Add((Status.FetchedFileBytes, new ReadOnlyMemory<byte>(fileBytes, offset, read)),
                    CancellationToken.None);
                offset += read;
            } while (fileBytes!.Length != offset);

            _readingQueue.Add((Status.FetchedFileBytes, ReadOnlyMemory<byte>.Empty), CancellationToken.None);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Handle exception on file bytes fetch");
            _readingQueue.Add((Status.ErrorOnFetchFileBytes, ReadOnlyMemory<byte>.Empty), CancellationToken.None);
            _ = _statistics.FileRxAsync(fileBytes.LongLength, RequestedFile, true);
            return;
        }

        _ = _statistics.FileRxAsync(fileBytes.LongLength, RequestedFile, false);

        try
        {
            await _fileManager.SaveFileAsync(RequestedFile, fileBytes);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Handle exception on file save");
            return;
        }
    }

    private async Task HathProxiedFileSendResponse(HttpContext ctx)
    {
        using var activity = Telemetry.ActivitySource.StartActivity();
        var response = ctx.Response;
        var ct = ctx.RequestAborted;
        do
        {
            var (status, mem) = _readingQueue.Take(ct);

            if (status == Status.Ok)
            {
                continue;
            }
            else if (status == Status.FetchedUrlsFromRpc)
            {
                continue;
            }
            else if (status == Status.ErrorOnFetchUrlsFromRpc)
            {
                await new HathTextResult(null, StatusCodes.Status404NotFound).ExecuteAsync(ctx);
                return;
            }
            else if (status == Status.FetchedFileHeaders)
            {
                activity?.AddTag("Size", mem.Length);
                response.StatusCode = StatusCodes.Status200OK;
                response.ContentType = RequestedFile.GetMimeType();
                response.ContentLength = mem.Length;
                response.Headers.CacheControl = "public, max-age=31536000";
                continue;
            }
            else if (status == Status.ErrorOnFetchFileHeaders)
            {
                await new HathTextResult(null, StatusCodes.Status502BadGateway).ExecuteAsync(ctx);
                return;
            }
            else if (status == Status.FetchedFileBytes)
            {
                if (mem.Length != 0)
                {
                    response.BodyWriter.Write(mem.Span);
                }
                else
                {
                    await response.BodyWriter.FlushAsync(ct);
                    break;
                }
            }
            else
            {
                _logger.LogError("Unknown state {name}", status.ToString());
            }
        } while (true);
    }

    private async Task<(IFlurlResponse response, int contentLength)> GetFileResponseAsync(
        IReadOnlyList<string> urls, CancellationToken ct = default)
    {
        foreach (var url in _downloadHelper.MapUrls(urls))
        {
            try
            {
                var request = _downloadHelper.GetRequest(url)
                    .WithTimeout(5000)
                    .AllowHttpStatus(200);
                var response = await request.GetAsync(HttpCompletionOption.ResponseHeadersRead, ct);
                var fileSize = int.Parse(response.Headers.FirstOrDefault("Content-Length"));
                if (fileSize == 0)
                    throw new Exception("Content length must be > 0");
                if (fileSize > _settingsStorage.Settings.MaxAllowedFileSize)
                    throw new Exception($"Content length must be < {_settingsStorage.Settings.MaxAllowedFileSize}");
                return (response, fileSize);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Fetch file ex");
            }
        }

        throw new Exception("All urls failed");
    }
}