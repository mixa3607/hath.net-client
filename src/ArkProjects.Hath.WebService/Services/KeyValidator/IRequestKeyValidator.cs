namespace ArkProjects.Hath.WebService.Services;

public interface IRequestKeyValidator
{
    bool CommandIsValid(long unixTime, string command, string additional, string? signature);
    bool TestIsValid(long unixTime, int testSize, string? signature);
    bool FileIsValid(long unixTime, string fileId, string? signature);
}