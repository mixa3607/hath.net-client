using ArkProjects.Hath.ClientApi;
using Quartz;

namespace ArkProjects.Hath.WebService.Quartz.Jobs;

public class ReloadBlacklistedJob : IJob
{
    private readonly ILogger<CertificateCheckJob> _logger;
    private readonly HathClient _client;

    public ReloadBlacklistedJob(ILogger<CertificateCheckJob> logger, HathClient client)
    {
        _logger = logger;
        _client = client;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var ct = context.CancellationToken;
        try
        {
            var resp = await _client.GetBlacklistAsync(TimeSpan.FromHours(72), ct);
            _logger.LogInformation("Blacklisted files received");
            if (resp.Lines.Count > 0)
            {
                throw new NotImplementedException();
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Control server reported error on blacklist fetch {message}",
                (e as HathClientException)?.Response.RawText);
        }
    }
}