using ArkProjects.Hath.WebService.Options;
using OpenTelemetry.Resources;

namespace ArkProjects.Hath.WebService.Services;

public interface ISettingsStorage : IResourceDetector
{
    IHathOptions Settings { get; }

    void Update(IReadOnlyDictionary<string, string> upds);
    void Update(string key, string value);

    void UpdateCert(byte[] bytes);
    int GetMaxConcurrentRequest();
}