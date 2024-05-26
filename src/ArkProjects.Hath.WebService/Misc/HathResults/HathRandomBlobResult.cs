using System.Net.Mime;

namespace ArkProjects.Hath.WebService.Misc;

public class HathRandomBlobResult : IResult
{
    private readonly long _size;
    private readonly int _statusCode;

    public HathRandomBlobResult(long size, int statusCode)
    {
        _size = size;
        _statusCode = statusCode;
    }

    public async Task ExecuteAsync(HttpContext httpContext)
    {
        var ct = httpContext.RequestAborted;
        var logger = httpContext.RequestServices.GetRequiredService<ILogger<HathRandomBlobResult>>();
        var response = httpContext.Response;
        response.ContentType = MediaTypeNames.Application.Octet;
        response.ContentLength = _size;
        response.StatusCode = _statusCode;

        var begin = DateTime.Now;
        var written = 0L;
        do
        {
            var mem = response.BodyWriter.GetMemory();
            var available = _size - written;
            var mustWrite = (int)(mem.Length > available ? available : mem.Length);
            Random.Shared.NextBytes(mem.Span[..mustWrite]);
            response.BodyWriter.Advance(mustWrite);
            written += mustWrite;
        } while (written != _size);

        await response.BodyWriter.FlushAsync(ct);

        var spend = DateTime.Now - begin;
        var speed = written / spend.TotalSeconds / 1024;
        logger.LogDebug("Write {bytes} bytes in {secs}s ({s} KB/s)", written, spend.TotalSeconds, speed);
    }
}