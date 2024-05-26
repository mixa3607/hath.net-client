using System.Diagnostics;
using Newtonsoft.Json;

namespace ArkProjects.Hath.ProtocolSamples;

public static class CurlResponseExecutor
{
    public static async Task<string> ExecuteAndSaveAsync(string args, string basePath, string name, string? description)
    {
        if (!Directory.Exists(basePath))
            Directory.CreateDirectory(basePath);

        var rawDumpName = $"{name}.raw.txt";
        var fullArgs = $"--max-time 60 -k -o - --trace-ascii {rawDumpName} {args}";
        Console.WriteLine(fullArgs);
        var startInfo = new ProcessStartInfo("curl", fullArgs)
        {
            CreateNoWindow = true,
            WorkingDirectory = basePath,
            RedirectStandardError = false,
            RedirectStandardOutput = false,
        };
        var process = Process.Start(startInfo)!;
        await process.WaitForExitAsync();
        var info = new ReqRespSampleInfoModel()
        {
            CreatedAt = DateTimeOffset.UtcNow,
            CurlArgs = startInfo.Arguments,
            CurlRetCode = process.ExitCode,
            Name = name,
            Description = description,
            RawDumpFile = rawDumpName
        };
        var jsonStr = JsonConvert.SerializeObject(info, Formatting.Indented);
        var infoFilePath = Path.Combine(basePath, $"{name}.info.json");
        await File.WriteAllTextAsync(infoFilePath, jsonStr);
        return infoFilePath;
    }
}