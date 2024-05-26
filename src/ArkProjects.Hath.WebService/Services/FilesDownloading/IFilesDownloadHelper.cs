using Flurl;
using Flurl.Http;

namespace ArkProjects.Hath.WebService.Services;

public interface IFilesDownloadHelper
{
    IReadOnlyList<Url> MapUrls(IReadOnlyList<string> urls);
    IFlurlRequest GetRequest(Url url);
}