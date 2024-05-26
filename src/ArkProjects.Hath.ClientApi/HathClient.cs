using ArkProjects.Hath.ClientApi.Constants;
using ArkProjects.Hath.ClientApi.Extensions;
using ArkProjects.Hath.ClientApi.Responses;
using Flurl;
using Flurl.Http;
using static System.Net.Mime.MediaTypeNames;
using System.IO;
using System;

namespace ArkProjects.Hath.ClientApi;

public class HathClient
{
    private readonly IHathClientOptions _options;
    private List<RpcServerInfo> _rpcServers = new();
    private DateTimeOffset _rpcServersLastUpdate;
    private RpcServerInfo? _selectedRpc;

    public IFlurlClient DefaultClient { get; }

    public HathClient(IHathClientOptions settingsStorage)
    {
        _options = settingsStorage;
        DefaultClient = new FlurlClient();
    }

    public Task<HathClientLinesResponse> DownloaderFailuresAsync(IReadOnlyList<FileDownloadFailure> failures,
        CancellationToken ct = default)
    {
        var add = string.Join(';', failures.Select(x => x.ToString()));
        return GetLinesAsync(HathRpcActions.DownloaderFailReport, add, ct);
    }

    public Task<HathClientLinesResponse> DownloaderFetchAsync(int galleryId, int page, int fileIndex, string xres,
        int attempt, CancellationToken ct = default)
    {
        var add = $"{galleryId};{page};{fileIndex};{xres};{attempt}";
        return GetLinesAsync(HathRpcActions.DownloaderFetch, add, ct);
    }

    public Task<HathClientLinesResponse> StaticRangeFetchAsync(int fileIndex, string xResType, string fileId,
        CancellationToken ct = default)
    {
        var add = $"{fileIndex};{xResType};{fileId}";
        return GetLinesAsync(HathRpcActions.StaticRangeFetch, add, ct);
    }

    public Task<HathClientPropsResponse> StillAliveAsync(bool resume, CancellationToken ct = default) =>
        GetPropertiesAsync(HathRpcActions.StillAlive, resume ? "resume" : "", ct);

    /// <summary>
    /// usually 72h or 12h
    /// </summary>
    public Task<HathClientLinesResponse> GetBlacklistAsync(TimeSpan delta, CancellationToken ct = default) =>
        GetLinesAsync(HathRpcActions.GetBlacklist, ((long)delta.TotalSeconds).ToString(), ct);

    public Task<HathClientLinesResponse> OverloadAsync(CancellationToken ct = default) =>
        GetLinesAsync(HathRpcActions.Overload, "", ct);

    public virtual Task<HathClientPropsResponse> ServerStatAsync(CancellationToken ct = default) =>
        GetPropertiesAsync(HathRpcActions.ServerStat, "", ct);

    public virtual Task<HathClientPropsResponse> ClientLoginAsync(CancellationToken ct = default) =>
        GetPropertiesAsync(HathRpcActions.ClientLogin, "", ct);

    public Task<HathClientPropsResponse> ClientSettingsAsync(CancellationToken ct = default) =>
        GetPropertiesAsync(HathRpcActions.ClientSettings, "", ct);

    public Task<HathClientLinesResponse> ClientStartAsync(CancellationToken ct = default) =>
        GetLinesAsync(HathRpcActions.ClientStart, "", ct);

    public Task<HathClientLinesResponse> ClientStopAsync(CancellationToken ct = default) =>
        GetLinesAsync(HathRpcActions.ClientStop, "", ct);

    public Task<HathClientLinesResponse> ClientSuspendAsync(CancellationToken ct = default) =>
        GetLinesAsync(HathRpcActions.ClientSuspend, "", ct);

    public Task<HathClientLinesResponse> ClientResumeAsync(CancellationToken ct = default) =>
        GetLinesAsync(HathRpcActions.ClientResume, "", ct);

    public virtual async Task<HathClientCertResponse> GetCertificateAsync(CancellationToken ct = default)
    {
        var rpcHost = GetRpcHost();
        try
        {
            var resp = await MakeRequestAsync(rpcHost, "/15/rpc", HathRpcActions.GetCertificate, "", ct);
            var bytes = await resp.GetBytesAsync();
            return new HathClientCertResponse()
            {
                Success = true,
                Status = "OK",
                CertBytes = bytes,
                StatusCode = resp.StatusCode,
            };
        }
        catch (Exception e) when (e is not HathClientException)
        {
            var result = new HathClientCertResponse()
            {
                Success = false,
                Status = "FAIL",
                StatusCode = (e as FlurlHttpException)?.StatusCode ?? -1,
            };
            throw new HathClientException($"Exception on fetch cert ({result.Status})", result, e);
        }
    }

    /// <summary>
    /// INVALID_REQUEST, NO_PENDING_DOWNLOADS
    /// </summary>
    public virtual async Task<HathClientDownloadGalleryResponse> GetDownloadQueueAsync(int? galleryId, string? xres,
        CancellationToken ct = default)
    {
        string? text = null;
        var rpcHost = GetRpcHost();
        try
        {
            var add = galleryId != null && xres != null ? $"{galleryId};{xres}" : "";
            var resp = await MakeRequestAsync(rpcHost, "/15/dl", HathDownloadActions.FetchQueue, add, ct);
            text = await resp.GetStringAsync();
            var lines = text.Split('\n', '\r').Where(x => !string.IsNullOrEmpty(x)).ToArray();

            string status;
            if (lines.Length == 0)
            {
                status = "EMPTY_RESPONSE";
            }
            else if (lines[0] == "INVALID_REQUEST")
            {
                status = "INVALID_REQUEST";
            }
            else if (lines[0] == "NO_PENDING_DOWNLOADS")
            {
                status = "NO_PENDING_DOWNLOADS";
            }
            else
            {
                status = "OK";
            }

            var result = new HathClientDownloadGalleryResponse()
            {
                Status = status,
                RawText = text,
                StatusCode = resp.StatusCode,
                Success = status == "OK",
                Gallery = HathGalleryInfo.Parse(lines)
            };

            return result.Success
                ? result
                : throw new HathClientException($"Server return non OK result ({result.Status})", result);
        }
        catch (Exception e) when (e is not HathClientException)
        {
            var result = new HathClientDownloadGalleryResponse()
            {
                Success = false,
                Status = "FAIL",
                StatusCode = (e as FlurlHttpException)?.StatusCode ?? -1,
                RawText = text,
            };
            throw new HathClientException($"Exception on fetch galleries ({result.Status})", result, e);
        }
    }

    private Task<HathClientLinesResponse> GetLinesAsync(string action, string additional,
        CancellationToken ct = default) => GetLinesAsync(action, additional, "/15/rpc", ct);

    private async Task<HathClientLinesResponse> GetLinesAsync(string action, string additional, string path,
        CancellationToken ct = default)
    {
        string? text = null;
        var rpcHost = GetRpcHost();
        try
        {
            var resp = await MakeRequestAsync(rpcHost, path, action, additional, ct);
            text = await resp.GetStringAsync();
            var lines = text.Split('\n', '\r').Where(x => !string.IsNullOrEmpty(x)).ToArray();

            var status = lines.Length == 0 ? "EMPTY_RESPONSE" : lines[0];
            var result = new HathClientLinesResponse()
            {
                Success = status == "OK",
                Lines = lines.Length > 1
                    ? lines.Skip(1).ToArray()
                    : Array.Empty<string>(),
                RawText = text,
                Status = status,
                StatusCode = resp.StatusCode
            };

            return result.Success
                ? result
                : throw new HathClientException($"Server return non OK result ({result.Status})", result);
        }
        catch (Exception e)
        {
            MarkRpcHostFailed(rpcHost);
            if (e is HathClientException)
                throw;
            var result = new HathClientLinesResponse()
            {
                Success = false,
                Lines = Array.Empty<string>(),
                RawText = text,
                Status = "FAIL",
                StatusCode = (e as FlurlHttpException)?.StatusCode ?? -1,
            };
            throw new HathClientException($"Exception on fetch lines ({result.Status})", result, e);
        }
    }

    private Task<HathClientPropsResponse> GetPropertiesAsync(string action, string additional,
        CancellationToken ct = default) => GetPropertiesAsync(action, additional, "/15/rpc", ct);

    private async Task<HathClientPropsResponse> GetPropertiesAsync(string action, string additional, string path,
        CancellationToken ct = default)
    {
        string? text = null;
        var rpcHost = GetRpcHost();
        try
        {
            var resp = await MakeRequestAsync(rpcHost, path, action, additional, ct);
            text = await resp.GetStringAsync();
            var (status, msg, props) = ParsePropertiesResponse(text);
            var result = new HathClientPropsResponse()
            {
                Success = status == "OK",
                StatusCode = resp.StatusCode,
                RawText = text,
                Status = status,
                Message = msg,
                Properties = props,
            };
            return result.Success
                ? result
                : throw new HathClientException($"Server return non OK result ({result.Status})", result);
        }
        catch (Exception e)
        {
            MarkRpcHostFailed(rpcHost);
            if (e is HathClientException)
                throw;
            var result = new HathClientPropsResponse()
            {
                Success = false,
                Properties = new Dictionary<string, string>(0),
                RawText = text,
                Status = "FAIL",
                StatusCode = (e as FlurlHttpException)?.StatusCode ?? -1,
            };
            throw new HathClientException($"Exception on fetch lines ({result.Status})", result, e);
        }
    }

    private async Task<IFlurlResponse> MakeRequestAsync(string rpcHost, string segment, string action,
        string additional,
        CancellationToken ct = default)
    {
        var req = new FlurlRequest($"http://{rpcHost}".AppendPathSegment(segment));
        req.Client = DefaultClient;
        SetQueryParams(req, action, additional);

        var resp = await req.GetAsync(cancellationToken: ct);
        return resp;
    }

    private void MarkRpcHostFailed(string host)
    {
        lock (_rpcServers)
        {
            var inf = _rpcServers.FirstOrDefault(x => x.Host == host);
            if (inf != null)
            {
                inf.LastFail = DateTimeOffset.UtcNow;
                if (_selectedRpc == inf)
                {
                    _selectedRpc = null;
                }
            }
        }
    }

    private string GetRpcHost()
    {
        lock (_rpcServers)
        {
            if (_rpcServersLastUpdate != _options.RpcServersLastUpdate)
            {
                var newHosts = _options.RpcServerAddresses
                    .Select(x => _rpcServers.FirstOrDefault(o => o.Host == x) ?? new RpcServerInfo(x))
                    .ToArray();
                _rpcServers.Clear();
                _rpcServers.AddRange(newHosts);
                if (_selectedRpc != null && !_rpcServers.Contains(_selectedRpc))
                {
                    _selectedRpc = null;
                }
            }

            if (_selectedRpc != null)
                return _selectedRpc.Host;

            var allowedRpcHosts = _rpcServers.Where(x => x.IsAllowedToUse()).ToArray();
            if (allowedRpcHosts.Length == 0)
                return _options.DefaultHost;

            var rng = Random.Shared.Next(0, allowedRpcHosts.Length);
            _selectedRpc = allowedRpcHosts[rng];
            return _selectedRpc.Host;
        }
    }

    protected (string status, string? message, Dictionary<string, string> props) ParsePropertiesResponse(string resp)
    {
        var lines = resp
            .Split('\n', '\r')
            .Where(x => !string.IsNullOrEmpty(x))
            .ToArray();
        var status = lines.FirstOrDefault() ?? "";
        var message = (string?)null;
        if (lines.Length == 2)
        {
            message = lines[1];
        }

        var props = lines
            .Skip(1)
            .Select(x => x.Split('=', 2))
            .Where(x => x.Length == 2)
            .ToDictionary(x => x[0], x => x[1]);
        return (status, message, props);
    }

    protected void SetQueryParams(IFlurlRequest req, string action, string additional)
    {
        if (action == HathRpcActions.ServerStat)
        {
            req.SetQueryParams(new Dictionary<string, object>()
            {
                { "clientbuild", _options.CurrentClientBuild },
                { "act", action },
            });
        }
        else
        {
            var serverTime = DateTimeOffset.Now.Add(_options.ServerTimeDelta).ToUnixTimeSeconds();
            var actKeyStr =
                $"hentai@home-{action}-{additional}-{_options.ClientId}-{serverTime}-{_options.ClientKey}";
            var actKeySha = actKeyStr.GetSha1AsStr();
            req.SetQueryParams(new Dictionary<string, object>()
            {
                { "clientbuild", _options.CurrentClientBuild },
                { "act", action },
                { "add", additional },
                { "cid", _options.ClientId },
                { "acttime", serverTime },
                { "actkey", actKeySha }
            });
        }
    }
}