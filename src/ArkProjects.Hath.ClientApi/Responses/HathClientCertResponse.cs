namespace ArkProjects.Hath.ClientApi.Responses;

public class HathClientCertResponse : IHathClientResponse
{
    public bool Success { get; set; }
    public required int StatusCode { get; set; }
    public required string Status { get; set; }
    public string? RawText { get; }
    public string? Message { get; set; }
    public byte[]? CertBytes { get; set; }
}