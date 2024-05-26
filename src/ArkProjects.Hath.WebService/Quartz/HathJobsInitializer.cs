using ArkProjects.Hath.WebService.Quartz.Jobs;
using Quartz;

namespace ArkProjects.Hath.WebService.Quartz;

public static class HathJobsInitializer
{
    public static IServiceCollectionQuartzConfigurator AddHathJobs(
        this IServiceCollectionQuartzConfigurator c)
    {
        {
            var key = nameof(StillAliveNotifyJob);
            c.AddJob<StillAliveNotifyJob>(j => j
                .DisallowConcurrentExecution()
                .WithIdentity(key)
            );
            c.AddTrigger(t => t
                .ForJob(key)
                .WithCronSchedule("0 0/2 * ? * * *")
                .StartAt(DateTimeOffset.UtcNow.AddMinutes(2))
            );
        }

        {
            var key = nameof(CertificateCheckJob);
            c.AddJob<CertificateCheckJob>(j => j
                .DisallowConcurrentExecution()
                .WithIdentity(key)
            );
            // every hour
            c.AddTrigger(t => t
                .ForJob(key)
                .WithCronSchedule("0 0 * ? * * *")
                .StartAt(DateTimeOffset.UtcNow.AddHours(12))
            );
        }

        {
            var key = nameof(ReloadBlacklistedJob);
            c.AddJob<ReloadBlacklistedJob>(j => j
                .DisallowConcurrentExecution()
                .WithIdentity(key)
            );
            // every hour
            c.AddTrigger(t => t
                .ForJob(key)
                .WithCronSchedule("0 0 * ? * * *")
            );
        }

        {
            var key = nameof(DownloadPendingGalleriesJob);
            c.AddJob<DownloadPendingGalleriesJob>(j => j
                .DisallowConcurrentExecution()
                .WithIdentity(key)
            );
            // every hour
            c.AddTrigger(t => t
                .ForJob(key)
                .WithCronSchedule("0 0 * ? * * *")
                .StartAt(DateTimeOffset.UtcNow.AddMinutes(2))
            );
        }
        {
            var key = nameof(CacheDriveFreeSpaceUpdateJob);
            c.AddJob<CacheDriveFreeSpaceUpdateJob>(j => j
                .DisallowConcurrentExecution()
                .WithIdentity(key)
            );
            // every 30 sec
            c.AddTrigger(t => t
                .ForJob(key)
                .WithCronSchedule("0/30 * 0 ? * * *")
                .StartNow()
            );
        }

        return c;
    }
}