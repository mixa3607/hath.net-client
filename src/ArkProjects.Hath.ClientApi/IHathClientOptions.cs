namespace ArkProjects.Hath.ClientApi;

public interface IHathClientOptions
{
    int MinimalClientBuild { get; }
    int CurrentClientBuild { get;}
    int LatestClientBuild { get; }

    int ClientId { get; }
    string ClientKey { get; }
    TimeSpan ServerTimeDelta { get; }

    string DefaultHost { get; }
    List<string> RpcServerAddresses { get; set; }
    DateTimeOffset RpcServersLastUpdate { get; set; }
}