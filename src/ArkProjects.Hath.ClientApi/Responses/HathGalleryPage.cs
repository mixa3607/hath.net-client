namespace ArkProjects.Hath.ClientApi.Responses;

public class HathGalleryPage
{
    public required int FileIndex { get; init; }
    public required string XRes { get; init; }
    public required string? Hash { get; init; }
    public required string Type { get; init; }
    public required string FileName { get; init; }
}