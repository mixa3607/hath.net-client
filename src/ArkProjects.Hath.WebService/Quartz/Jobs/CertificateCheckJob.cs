using ArkProjects.Hath.ClientApi;
using ArkProjects.Hath.WebService.Services;
using Quartz;

namespace ArkProjects.Hath.WebService.Quartz.Jobs;

public class CertificateCheckJob : IJob
{
    private readonly ILogger<CertificateCheckJob> _logger;
    private readonly HathClient _client;
    private readonly ISettingsStorage _settingsStorage;
    private readonly IHathServerLifetimeService _hathServerLifetime;

    public CertificateCheckJob(ILogger<CertificateCheckJob> logger, HathClient client,
        ISettingsStorage settingsStorage, IHathServerLifetimeService hathServerLifetime)
    {
        _logger = logger;
        _client = client;
        _settingsStorage = settingsStorage;
        _hathServerLifetime = hathServerLifetime;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var ct = context.CancellationToken;
        _logger.LogDebug("Begin check certificate");
        var currCert = _settingsStorage.Settings.Certificate;
        var reload = false;
        if (_hathServerLifetime.ServerStatus != HathServerStatus.Running)
        {
            _logger.LogWarning("Server not started. Ignore SSL cert update");
            reload = false;
        }
        else if (currCert == null)
        {
            _logger.LogWarning("Current SSL certificate not set");
            reload = true;
        }
        else if (currCert.NotAfter < DateTime.Now.AddDays(-1))
        {
            _logger.LogWarning("Current SSL certificate expired");
            reload = true;
        }

        if (reload)
        {
            var resp = await _client.GetCertificateAsync(ct);
            _settingsStorage.UpdateCert(resp.CertBytes!);
            _logger.LogInformation("Certificate updated. {cert}",
                _settingsStorage.Settings.Certificate!.SubjectName.Name);
        }
    }
}