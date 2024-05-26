namespace ArkProjects.Hath.ClientApi.Responses;

public class HathClientDownloadGalleryResponse : IHathClientResponse
{
    public bool Success { get; set; }
    public required int StatusCode { get; set; }
    public required string Status { get; set; }
    public required string? RawText { get; set; }
    public HathGalleryInfo? Gallery { get; set; }
}