namespace ArkProjects.Hath.ClientApi;

public class FileDownloadFailure
{
    public required string Host { get; set; }
    public required string XRes { get; init; }
    public required int FileIndex { get; init; }

    public override string ToString()
    {
        return $"{Host}-{FileIndex}-{XRes}";
    }
}