namespace ArkProjects.Hath.WebService.Services;

public class FileManagerOptions
{
    public string CacheStoragePath { get; set; } = "./files/cache";
    public string TempStoragePath { get; set; } = "./files/tmp";
    public string StateFilePath { get; set; } = "./files/file_manager_state.json";
}