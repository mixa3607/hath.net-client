using System.Net.Mime;
using System.Text;

namespace ArkProjects.Hath.WebService.Misc;

public class HathTextResult : IResult
{
    private readonly int _statusCode;
    private readonly string? _text;
    private readonly string _mimeType;
    private readonly Encoding _encoding;

    public HathTextResult(string? text, int statusCode, string? mimeType = null, Encoding? encoding = null)
    {
        _statusCode = statusCode;
        _text = text;
        _mimeType = mimeType ?? MediaTypeNames.Text.Html;
        _encoding = encoding ?? Encoding.UTF8;
    }

    public Task ExecuteAsync(HttpContext httpContext)
    {
        var text = _statusCode == StatusCodes.Status200OK
            ? _text
            : $"An error has occurred. ({_statusCode})";
        return TypedResults.Text(text, _mimeType, _encoding, _statusCode)
            .ExecuteAsync(httpContext);
    }
}