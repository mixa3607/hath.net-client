using System.Net;
using System.Security.Cryptography.X509Certificates;
using ArkProjects.Hath.WebService.Services;

namespace ArkProjects.Hath.WebService.Options;

public class HathServerOptions : IHathOptions
{
    public string DefaultHost { get; } = "rpc.hentaiathome.net";
    public int ClientId { get; set; }
    public string ClientKey { get; set; } = "";
    public TimeSpan ServerTimeDelta { get; set; }
    public List<string> RpcServerAddresses { get; set; } = new();
    public DateTimeOffset RpcServersLastUpdate { get; set; }

    //listener
    public int Port { get; set; }
    public IPAddress Host { get; set; } = IPAddress.IPv6Any;

    //client build
    public int MinimalClientBuild { get; set; }
    public int CurrentClientBuild { get; set; } = 168;
    public int LatestClientBuild { get; set; }

    //net control
    public long ThrottleBytes { get; set; }
    public long DiskLimitBytes { get; set; }
    public long DiskRemainingBytes { get; set; }
    public bool DisableBwm { get; set; }

    //files settings
    public int MaxAllowedFileSize { get; set; } = 1073741824;
    public HathServerStatus ServerStatus { get; set; }
    public List<string> StaticRanges { get; set; } = new();

    //hosting settings
    public List<IPNetwork> CustomRpcServerNetworks { get; set; } = new();
    public X509Certificate2? Certificate { get; set; }

    public bool IgnoreAddressFromServer { get; set; }
    public bool NoStopNotify { get; set; }
    public bool NoStartNotify { get; set; }

    //request validation settings
    public bool IgnoreRequestInvalidTime { get; set; }
    public bool IgnoreRequestInvalidSignature { get; set; }

    public int MaxConcurrentRequestOverride { get; set; }
}