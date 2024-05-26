namespace ArkProjects.Hath.WebService.Options;

public class OpenTelemetryOptions
{
    public bool Enable { get; set; }

    public bool EnableMetricsToPrometheus { get; set; }
    public int[]? PrometheusPorts { get; set; }

    public bool EnableTracingToConsole { get; set; }
    public bool EnableTracingToOtlp { get; set; }
}