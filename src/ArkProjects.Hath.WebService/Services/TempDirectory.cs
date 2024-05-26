namespace ArkProjects.Hath.WebService.Services;

public class TempDirectory : IDisposable
{
    public TempDirectory(string path)
    {
        Path = path;
    }

    public string Path { get; }

    public void Delete()
    {
        if (Directory.Exists(Path))
        {
            Directory.Delete(Path, true);
        }
    }

    public void Create()
    {
        if (!Directory.Exists(Path))
        {
            Directory.CreateDirectory(Path);
        }
    }
    public void Move(string dst)
    {
        Directory.Move(Path, dst);
    }

    public void Dispose()
    {
        try
        {
            Delete();
        }
        catch (Exception e)
        {
            //ignore?
        }
    }
}