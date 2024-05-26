using ArkProjects.Hath.WebService.Services;

namespace ArkProjects.Hath.WebService.Misc;

public class HathPhysicalFileResult : IHathFileResult
{
    public RequestedFile RequestedFile { get; }
    public string FilePath { get; }

    private ILogger<HathPhysicalFileResult> _logger = null!;

    public HathPhysicalFileResult(string filePath, RequestedFile requestedFile)
    {
        FilePath = filePath;
        RequestedFile = requestedFile;
    }

    public async Task ExecuteAsync(HttpContext ctx)
    {
        using var activity = Telemetry.ActivitySource.StartActivity();
        _logger = ctx.RequestServices.GetRequiredService<ILogger<HathPhysicalFileResult>>();
        var withError = false;
        try
        {
            await HathPhysicalFileSendResponse(ctx.Response, ctx.RequestAborted);
        }
        catch (Exception e)
        {
            withError = true;
            throw;
        }
        finally
        {
            var stat = ctx.RequestServices.GetRequiredService<IStatisticsManager>();
            _ = stat.FileTxAsync(ctx.Connection.RemoteIpAddress!, ctx.Response.ContentLength ?? 0, RequestedFile, withError);
        }
    }

    private async Task HathPhysicalFileSendResponse(HttpResponse response, CancellationToken ct)
    {
        using var activity = Telemetry.ActivitySource.StartActivity();
        var begin = DateTime.Now;
        await using var fileStream = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 0,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

        activity?.AddTag("Size", fileStream.Length);
        response.StatusCode = StatusCodes.Status200OK;
        response.ContentType = RequestedFile.GetMimeType();
        response.ContentLength = fileStream.Length;
        response.Headers.CacheControl = "public, max-age=31536000";

        await HathPhysicalFileCopyToResponse(fileStream, response, ct);
        await response.BodyWriter.FlushAsync(ct);

        var spend = DateTime.Now - begin;
        var speed = fileStream.Length / spend.TotalSeconds / 1024;
        activity?.AddTag("Speed", speed);
        _logger.LogDebug("Write {bytes} bytes in {secs}s ({s} KB/s)", fileStream.Length, spend.TotalSeconds, speed);
    }

    private async Task HathPhysicalFileCopyToResponse(Stream fileStream, HttpResponse response, CancellationToken ct)
    {
        using var activity = Telemetry.ActivitySource.StartActivity();
        activity?.AddTag("Size", fileStream.Length);
        var begin = DateTime.Now;
        do
        {
            if (ct.IsCancellationRequested)
                throw new OperationCanceledException();

            var mem = response.BodyWriter.GetMemory();
            var available = fileStream.Length - fileStream.Position;
            var read = available > mem.Length ? mem.Length : (int)available;
            _ = await fileStream.ReadAsync(mem[..read], ct);
            response.BodyWriter.Advance(read);
        } while (fileStream.Length != fileStream.Position);

        var spend = DateTime.Now - begin;
        var speed = fileStream.Length / spend.TotalSeconds / 1024;
        activity?.AddTag("Speed", speed);
    }
}