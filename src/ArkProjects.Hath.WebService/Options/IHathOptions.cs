using System.Net;
using System.Security.Cryptography.X509Certificates;
using ArkProjects.Hath.ClientApi;

namespace ArkProjects.Hath.WebService.Options;

public interface IHathOptions : IHathClientOptions
{
    int Port { get; set; }
    IPAddress Host { get; set; }

    List<IPNetwork> CustomRpcServerNetworks { get; set; }

    public X509Certificate2? Certificate { get; set; }

    bool NoStartNotify { get; set; }
    bool NoStopNotify { get; set; }
    bool IgnoreRequestInvalidTime { get; set; }
    bool IgnoreRequestInvalidSignature { get; set; }
    bool IgnoreAddressFromServer { get; set; }


    //net control
    long ThrottleBytes { get; set; }
    long DiskLimitBytes { get; set; }
    long DiskRemainingBytes { get; set; }
    bool DisableBwm { get; set; }

    int MaxAllowedFileSize { get; set; }
}