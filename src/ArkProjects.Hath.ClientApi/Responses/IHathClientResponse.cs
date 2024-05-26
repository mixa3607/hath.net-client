namespace ArkProjects.Hath.ClientApi.Responses;

public interface IHathClientResponse
{
    public bool Success { get; }
    int StatusCode { get; }
    string Status { get; }
    string? RawText { get; }
}