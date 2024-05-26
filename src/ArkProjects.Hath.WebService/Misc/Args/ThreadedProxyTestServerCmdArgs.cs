namespace ArkProjects.Hath.WebService.Misc;

public class ThreadedProxyTestServerCmdArgs
{
    public required string HostName { get; set; }
    public required string? Protocol { get; set; }
    public required int Port { get; set; }
    public required int TestSize { get; set; }
    public required int TestCount { get; set; }
    public required long TestTime { get; set; }
    public required string TestKey { get; set; }


    public static ThreadedProxyTestServerCmdArgs Parse(string additional)
    {
        var props = additional
            .Split(';')
            .Where(x => x.Length > 2)
            .Select(x => x.Split('=', 2, StringSplitOptions.TrimEntries))
            .Where(x => x.Length == 2)
            .ToDictionary(k => k[0], v => v[1]);

        var args = new ThreadedProxyTestServerCmdArgs()
        {
            HostName = props["hostname"],
            Protocol = props.FirstOrDefault(x => x.Key == "protocol").Value ?? "https",
            Port = int.Parse(props["port"]),
            TestSize = int.Parse(props["testsize"]),
            TestCount = int.Parse(props["testcount"]),
            TestTime = long.Parse(props["testtime"]),
            TestKey = props["testkey"],
        };

        return args;
    }
}
