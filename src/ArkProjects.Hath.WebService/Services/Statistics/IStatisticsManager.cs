using System.Net;
using ArkProjects.Hath.WebService.Misc;
using OpenTelemetry.Resources;

namespace ArkProjects.Hath.WebService.Services;

public interface IStatisticsManager: IResourceDetector
{
    Task InitAsync(CancellationToken ct = default);
    Task FileTxAsync(IPAddress remoteIp, long size, RequestedFile file, bool error);
    Task FileRxAsync(long size, RequestedFile file, bool error);
    Task ConnectionsOpenAsync(long count);
}