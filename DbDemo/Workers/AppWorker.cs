using DbDemo.Services;

namespace DbDemo.Workers;

public class AppWorker(CacheService cache) : BackgroundService
{
    private readonly CacheService cache = cache;

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        cache.AppStartTime = DateTime.Now;
        return Task.CompletedTask;
    }
}
