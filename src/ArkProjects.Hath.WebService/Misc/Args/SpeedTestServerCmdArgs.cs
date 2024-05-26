namespace ArkProjects.Hath.WebService.Misc;

public class SpeedTestServerCmdArgs
{
    public int? TestSize { get; set; }

    public static SpeedTestServerCmdArgs Parse(string additional)
    {
        var props = additional
            .Split(';')
            .Where(x => x.Length > 2)
            .Select(x => x.Split('=', 2, StringSplitOptions.TrimEntries))
            .Where(x => x.Length == 2)
            .ToDictionary(k => k[0], v => v[1]);

        var args = new SpeedTestServerCmdArgs();
        if (props.TryGetValue("testsize", out var sS) && int.TryParse(sS, out var sI))
            args.TestSize = sI;

        return args;
    }
}