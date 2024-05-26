namespace ArkProjects.Hath.ClientApi.Constants;

public static class SettingNames
{
    // server_stat
    public const string MinimalClientBuild = "min_client_build";
    public const string CurrentClientBuild = "cur_client_build";
    public const string ServerTime = "server_time";

    // client_login, client_settings
    public const string RpcServerIp = "rpc_server_ip";
    public const string Host = "host";
    public const string Port = "port";
    public const string ThrottleBytes = "throttle_bytes";
    public const string DiskLimitBytes = "disklimit_bytes";
    public const string DiskRemainingBytes = "diskremaining_bytes";
    public const string DisableBwm = "disable_bwm";
    public const string StaticRanges = "static_ranges";
}