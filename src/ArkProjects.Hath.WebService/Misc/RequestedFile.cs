using System.Net.Mime;
using System.Text.RegularExpressions;

namespace ArkProjects.Hath.WebService.Misc;

public class RequestedFile
{
    public required string Hash { get; init; }
    public required int Size { get; init; }
    public required int XRes { get; init; }
    public required int YRes { get; init; }
    public required string Type { get; init; }

    public string? FileName { get; set; }
    public int FileIndex { get; set; } = -1;
    public string? XResType { get; set; }

    private string? _fileId;
    private string? _relPath;
    private string? _staticRange;
    private string? _mimeType;

    public static RequestedFile Parse(string fileId)
    {
        if (!Regex.IsMatch(fileId, "^[a-f0-9]{40}-[0-9]{1,8}-[0-9]{1,5}-[0-9]{1,5}-((jpg)|(png)|(gif)|(wbm))$"))
            throw new Exception($"Invalid file id {fileId}");

        var parts = fileId.Split('-');
        return new RequestedFile()
        {
            Hash = parts[0],
            Size = int.Parse(parts[1]),
            XRes = int.Parse(parts[2]),
            YRes = int.Parse(parts[3]),
            Type = parts[4]
        };
    }

    public string GetMimeType()
    {
        return _mimeType ??= Type switch
        {
            "jpg" => MediaTypeNames.Image.Jpeg,
            "png" => MediaTypeNames.Image.Png,
            "gif" => MediaTypeNames.Image.Gif,
            "wbm" => "video/webm",
            _ => MediaTypeNames.Application.Octet
        };
    }

    public string GetFileId()
    {
        return _fileId ??= $"{Hash}-{Size}-{XRes}-{YRes}-{Type}";
    }

    public string GetRelativePath()
    {
        return _relPath ??= $"{Hash.Substring(0, 2)}/{Hash.Substring(2, 2)}/{GetFileId()}";
    }

    public string GetStaticRange()
    {
        return _staticRange ??= Hash.Substring(0, 4);
    }
}