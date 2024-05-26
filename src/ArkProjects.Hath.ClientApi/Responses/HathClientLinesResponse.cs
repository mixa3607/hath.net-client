namespace ArkProjects.Hath.ClientApi.Responses;

public class HathClientLinesResponse : IHathClientResponse
{
    public bool Success { get; set; }
    public required int StatusCode { get; set; }
    public required string Status { get; set; }
    public required string? RawText { get; set; }
    public required IReadOnlyList<string> Lines { get; set; }
}