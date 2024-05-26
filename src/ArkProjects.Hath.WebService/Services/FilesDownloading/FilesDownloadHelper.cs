using System.Net;
using System.Net.Security;
using ArkProjects.Hath.WebService.Options;
using Flurl;
using Flurl.Http;
using Microsoft.Extensions.Options;

namespace ArkProjects.Hath.WebService.Services;

public class FilesDownloadHelper : IFilesDownloadHelper
{
    private readonly FileDownloadingOptions _options;
    private IFlurlClient? _client;

    public FilesDownloadHelper(IOptions<FileDownloadingOptions> options)
    {
        _options = options.Value;
    }

    public IReadOnlyList<Url> MapUrls(IReadOnlyList<string> urls)
    {
        if (_options.UrlMappingMode is FilesDownloadingUrlMappingMode.Default
            or FilesDownloadingUrlMappingMode.ForceSsl)
        {
            return urls.Select(x =>
            {
                var u = new Url(x);
                if (u.Scheme == "http" && _options.UrlMappingMode == FilesDownloadingUrlMappingMode.ForceSsl)
                {
                    u.Scheme = "https";
                    if (u.Port == 80)
                        u.Port = 443;
                }

                return u;
            }).ToArray();
        }
        else if (_options.UrlMappingMode == FilesDownloadingUrlMappingMode.PreferSsl)
        {
            var newUrls = new Url?[urls.Count * 2];
            for (int i = 0; i < urls.Count; i++)
            {
                var url = new Url(urls[i]);
                if (url.Scheme == "https")
                {
                    newUrls[i] = url;
                }
                else
                {
                    newUrls[i] = url;
                    newUrls[i]!.Scheme = "https";
                    if (newUrls[i]!.Port is 80)
                        newUrls[i]!.Port = 443;
                    newUrls[i + urls.Count] = url;
                }
            }

            return newUrls.Where(x => x != null).ToArray() as Url[];
        }
        else
        {
            throw new NotSupportedException($"File downloading mode {_options.UrlMappingMode} not supported");
        }
    }

    public IFlurlRequest GetRequest(Url url)
    {
        var r = new FlurlRequest(url);
        r.Client = _client ??= CreateClient();
        return r;
    }

    private IFlurlClient CreateClient()
    {
        return new FlurlClient(new HttpClient(new HttpClientHandler()
        {
            Proxy = CreateProxy(),
            UseProxy = _options.UseProxy,
            ServerCertificateCustomValidationCallback = (message, cert, certChain, sslPolicy) =>
            {
                if (sslPolicy == SslPolicyErrors.None)
                {
                    return true;
                }
                else if (sslPolicy == SslPolicyErrors.RemoteCertificateNameMismatch
                         && _options.SslCheckMode == FilesDownloadingSslCheckMode.AllowNameMismatch)
                {
                    return true;
                }
                else if (_options.SslCheckMode == FilesDownloadingSslCheckMode.Bypass)
                {
                    return true;
                }

                return false;
            }
        }));
    }

    private IWebProxy? CreateProxy()
    {
        if (!_options.UseProxy)
            return null;
        var proxy = new WebProxy(new Uri(_options.ProxyUrl!));

        if (_options is { ProxyLogin: not null, ProxyPassword: not null })
            proxy.Credentials = new NetworkCredential(_options.ProxyLogin, _options.ProxyPassword);

        return proxy;
    }
}