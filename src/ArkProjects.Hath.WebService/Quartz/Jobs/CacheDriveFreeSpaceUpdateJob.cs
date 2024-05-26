using ArkProjects.Hath.WebService.Services;
using Quartz;

namespace ArkProjects.Hath.WebService.Quartz.Jobs;

public class CacheDriveFreeSpaceUpdateJob : IJob
{
    private readonly IFileManager _fileManager;

    public CacheDriveFreeSpaceUpdateJob(IFileManager fileManager)
    {
        _fileManager = fileManager;
    }

    public Task Execute(IJobExecutionContext context)
    {
        _fileManager.UpdateCacheDriveFreeSpace();
        return Task.CompletedTask;
    }
}