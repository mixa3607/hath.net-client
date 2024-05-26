namespace ArkProjects.Hath.ProtocolSamples;

public class ReqRespSampleInfoModel
{
    public int Version { get; } = 1;
    public required string CurlArgs { get; set; }
    public required int CurlRetCode { get; set; }
    public required string RawDumpFile { get; set; }
    public string? FilteredDumpFile { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }
}