using ArkProjects.Hath.ClientApi.Extensions;

namespace ArkProjects.Hath.WebService.Services;

public class RequestKeyValidator : IRequestKeyValidator
{
    private readonly ISettingsStorage _settingsStorage;
    private readonly TimeProvider _time;

    public RequestKeyValidator(ISettingsStorage settingsStorage, TimeProvider time)
    {
        _settingsStorage = settingsStorage;
        _time = time;
    }

    public bool TimeIsValid(long unixTime, int maxKeyTimeDrift)
    {
        if (_settingsStorage.Settings.IgnoreRequestInvalidTime)
            return true;

        var unixNow = _time.GetUtcNow().ToUnixTimeSeconds();
        var diff = Math.Abs(unixTime - unixNow);
        return diff < maxKeyTimeDrift;
    }

    public bool FileSignatureIsValid(long unixTime, string fileId, string? signature)
    {
        if (_settingsStorage.Settings.IgnoreRequestInvalidSignature)
            return true;

        var clientKey = _settingsStorage.Settings.ClientKey;
        var validSign = $"{unixTime}-{fileId}-{clientKey}-hotlinkthis".GetSha1AsStr()[..10];
        return validSign == signature;
    }

    public bool CommandSignatureIsValid(long unixTime, string command, string additional, string? signature)
    {
        if (_settingsStorage.Settings.IgnoreRequestInvalidSignature)
            return true;

        var clientId = _settingsStorage.Settings.ClientId;
        var clientKey = _settingsStorage.Settings.ClientKey;
        var validSign = $"hentai@home-servercmd-{command}-{additional}-{clientId}-{unixTime}-{clientKey}".GetSha1AsStr();
        return validSign == signature;
    }

    public bool TestSignatureIsValid(long unixTime, int testSize, string? signature)
    {
        if (_settingsStorage.Settings.IgnoreRequestInvalidSignature)
            return true;

        var clientId = _settingsStorage.Settings.ClientId;
        var clientKey = _settingsStorage.Settings.ClientKey;
        var validSign = $"hentai@home-speedtest-{testSize}-{unixTime}-{clientId}-{clientKey}".GetSha1AsStr();
        return validSign == signature;
    }

    public bool CommandIsValid(long unixTime, string command, string additional, string? signature)
    {
        var maxKeyTimeDrift = 300;
        return TimeIsValid(unixTime, maxKeyTimeDrift) &&
               CommandSignatureIsValid(unixTime, command, additional, signature);
    }

    public bool TestIsValid(long unixTime, int testSize, string? signature)
    {
        var maxKeyTimeDrift = 300;
        return TimeIsValid(unixTime, maxKeyTimeDrift) &&
               TestSignatureIsValid(unixTime, testSize, signature);
    }

    public bool FileIsValid(long unixTime, string fileId, string? signature)
    {
        var maxKeyTimeDrift = 900;
        return TimeIsValid(unixTime, maxKeyTimeDrift) &&
               FileSignatureIsValid(unixTime, fileId, signature);
    }
}