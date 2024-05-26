namespace ArkProjects.Hath.WebService.Misc;

public class GetFileArgs
{
    public required string Keystamp { get; set; }
    public required int FileIndex { get; set; }
    public required string Xres { get; set; }

    public long? Timestamp { get; set; }
    public string? Key { get; set; }

    public static GetFileArgs Parse(string additional)
    {
        var props = additional
            .Split(';')
            .Where(x => x.Length > 2)
            .Select(x => x.Split('=', 2, StringSplitOptions.TrimEntries))
            .Where(x => x.Length == 2)
            .ToDictionary(k => k[0], v => v[1]);

        var args = new GetFileArgs()
        {
            Keystamp = props["keystamp"],
            FileIndex = int.Parse(props["fileindex"]),
            Xres = props["xres"],
        };

        var parts = args.Keystamp.Split('-');
        if (parts.Length == 2)
        {
            if (!long.TryParse(parts[0], out var timestamp))
                return args;
            args.Timestamp = timestamp;
            args.Key = parts[1];
        }

        return args;
    }
}