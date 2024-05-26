namespace ArkProjects.Hath.WebService.Misc;

public class TestCommandResult
{
    public bool Success { get; set; }
    public int StatusCode { get; set; }
    public long ReadBytes { get; set; }
    public TimeSpan Elapsed { get; set; }
    public Exception? Exception { get; set; }
}