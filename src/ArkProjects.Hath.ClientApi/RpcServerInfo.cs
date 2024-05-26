namespace ArkProjects.Hath.ClientApi;

internal class RpcServerInfo
{
    public RpcServerInfo(string host)
    {
        Host = host;
    }

    public string Host { get; set; }
    public DateTimeOffset LastFail { get; set; }

    public bool IsAllowedToUse()
    {
        return DateTimeOffset.UtcNow - LastFail > TimeSpan.FromHours(4);
    }
}