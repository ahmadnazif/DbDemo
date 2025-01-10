
namespace DbDemo.Workers;

public class LoopWorker(ILogger<LoopWorker> logger) : BackgroundService
{
    private readonly ILogger<LoopWorker> logger = logger;
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            logger.LogInformation($"Called at {DateTime.Now.ToLongTimeString()}");
            await Task.Delay(2000, ct);
        }
    }
}
