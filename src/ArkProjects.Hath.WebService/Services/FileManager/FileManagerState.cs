using Newtonsoft.Json;

namespace ArkProjects.Hath.WebService.Services;

public class FileManagerState
{
    public long CacheSize { get; set; }
    public long CacheFilesCount { get; set; }

    public void AddFile(long size)
    {
        CacheFilesCount++;
        CacheSize += size;
    }

    public static FileManagerState Read(string file)
    {
        if (File.Exists(file))
        {
            return JsonConvert.DeserializeObject<FileManagerState>(File.ReadAllText(file))!;
        }

        return new FileManagerState();
    }

    public void Save(string file)
    {
        if (File.Exists(file))
        {
            File.Delete(file);
        }

        File.WriteAllText(file, JsonConvert.SerializeObject(this));
    }
}