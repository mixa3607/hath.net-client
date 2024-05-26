namespace ArkProjects.Hath.ClientApi.Responses;

public class HathClientPropsResponse: IHathClientResponse
{
    public bool Success { get; set; }
    public required int StatusCode { get; set; }
    public required string Status { get; set; }
    public string? Message { get; set; }
    public string? RawText { get; set; }
    public required IReadOnlyDictionary<string, string> Properties { get; set; }
}