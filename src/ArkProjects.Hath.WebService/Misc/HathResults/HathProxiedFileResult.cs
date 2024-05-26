using System.Buffers;
using System.Collections.Concurrent;
using ArkProjects.Hath.ClientApi;
using ArkProjects.Hath.WebService.Services;
using Flurl.Http;

namespace ArkProjects.Hath.WebService.Misc;

public class HathProxiedFileResult : IResult
{
    public RequestedFile RequestedFile { get; }

    private readonly BlockingCollection<ReadOnlyMemory<byte>> _readingQueue =
        new(new ConcurrentQueue<ReadOnlyMemory<byte>>());

    private ILogger<HathProxiedFileResult> _logger = null!;
    private ISettingsStorage _settingsStorage = null!;
    private IFileManager _fileManager = null!;
    private byte[]? _fileBytes;
    private IFilesDownloadHelper _downloadHelper = null!;

    public HathProxiedFileResult(RequestedFile requestedFile)
    {
        RequestedFile = requestedFile;
    }

    public async Task ExecuteAsync(HttpContext ctx)
    {
        var ct = ctx.RequestAborted;
        _logger = ctx.RequestServices.GetRequiredService<ILogger<HathProxiedFileResult>>();
        _fileManager = ctx.RequestServices.GetRequiredService<IFileManager>();
        _settingsStorage = ctx.RequestServices.GetRequiredService<ISettingsStorage>();
        _downloadHelper = ctx.RequestServices.GetRequiredService<IFilesDownloadHelper>();

        var withError = false;
        try
        {
            IReadOnlyList<string> urls;
            try
            {
                var hathClient = ctx.RequestServices.GetRequiredService<HathClient>();
                var resp = await hathClient.StaticRangeFetchAsync(
                    RequestedFile.FileIndex, RequestedFile.XResType!, RequestedFile.GetFileId(),
                    CancellationToken.None);

                if (resp.Lines.Count == 0)
                    throw new Exception("Fetch 0 urls");

                urls = resp.Lines;
            }
            catch (Exception e)
            {
                withError = true;
                _logger.LogError(e, "Handle exception on url fetch");
                await Results.StatusCode(StatusCodes.Status404NotFound).ExecuteAsync(ctx);
                return;
            }

            try
            {
                var result = await GetFileResponseAsync(urls, ct);
                _fileBytes = new byte[result.contentLength];
                _ = ReadStreamAndSaveAsync(result.response);
            }
            catch (Exception e)
            {
                withError = true;
                _logger.LogError(e, "Handle exception on file headers fetch");
                await Results.StatusCode(StatusCodes.Status502BadGateway).ExecuteAsync(ctx);
                return;
            }

            var begin = DateTime.Now;
            var response = ctx.Response;
            response.StatusCode = StatusCodes.Status200OK;
            response.ContentType = RequestedFile.GetMimeType();
            response.ContentLength = _fileBytes.Length;
            response.Headers.CacheControl = "public, max-age=31536000";
            response.Headers.ContentDisposition = "inline";

            HathProxiedFileCopyToResponse(response, ct);
            await response.BodyWriter.FlushAsync(ct);

            var spend = DateTime.Now - begin;
            var speed = _fileBytes.Length / spend.TotalSeconds / 1024;
            _logger.LogDebug("Write {bytes} bytes in {secs}s ({s} KB/s)", _fileBytes.Length, spend.TotalSeconds, speed);
        }
        catch (Exception e)
        {
            withError = true;
            throw;
        }
        finally
        {
            var stat = ctx.RequestServices.GetRequiredService<IStatisticsManager>();
            _ = stat.FileTxAsync(ctx.Connection.RemoteIpAddress!, ctx.Response.ContentLength ?? 0, RequestedFile,
                withError);
        }
    }

    private void HathProxiedFileCopyToResponse(HttpResponse response, CancellationToken ct)
    {
        using var activity = Telemetry.ActivitySource.StartActivity();
        activity?.AddTag("Size", _fileBytes!.Length);
        var begin = DateTime.Now;

        var wait = 0L;
        do
        {
            var a = DateTime.Now;
            var mem = _readingQueue.Take(ct);
            wait += DateTime.Now.Millisecond - a.Millisecond;

            if (mem.Length == 0)
            {
                _logger.LogDebug("File fetched");
                break;
            }

            response.BodyWriter.Write(mem.Span);
        } while (true);

        var spend = DateTime.Now - begin;
        var speed = _fileBytes!.Length / spend.TotalSeconds / 1024;
        activity?.AddTag("Speed", speed);
        activity?.AddTag("WaitMs", wait);
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

    private async Task ReadStreamAndSaveAsync(IFlurlResponse response)
    {
        try
        {
            _logger!.LogDebug("Begin reading stream");
            var offset = 0;
            var stream = await response.GetStreamAsync();
            do
            {
                var read = await stream.ReadAsync(_fileBytes!, offset, _fileBytes!.Length - offset);
                _readingQueue.Add(new ReadOnlyMemory<byte>(_fileBytes, offset, read));
                offset += read;
            } while (_fileBytes!.Length != offset);

            _readingQueue.Add(ReadOnlyMemory<byte>.Empty);

            await _fileManager.SaveFileAsync(RequestedFile, _fileBytes);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Cant receive file from hath server");
            _readingQueue.Add(ReadOnlyMemory<byte>.Empty);
        }
    }
}