namespace DbDemo.Workers;

public class DirectoryChecker(IWebHostEnvironment env, ILogger<DirectoryChecker> logger) : BackgroundService
{
    private readonly IWebHostEnvironment env = env;
    private readonly ILogger<DirectoryChecker> logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(1000, stoppingToken);

        try
        {
            var uploadFolder = Path.Combine(env.ContentRootPath, ServerFileHelper.FolderName.UserUploadFiles.ToString());
            if (!Directory.Exists(uploadFolder))
                Directory.CreateDirectory(uploadFolder);

            var archFolder = Path.Combine(env.ContentRootPath, ServerFileHelper.FolderName.UserArchiveFiles.ToString());
            if (!Directory.Exists(archFolder))
                Directory.CreateDirectory(archFolder);

            var genFolder = Path.Combine(env.ContentRootPath, ServerFileHelper.FolderName.UserGenFiles.ToString());
            if (!Directory.Exists(genFolder))
                Directory.CreateDirectory(genFolder);
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message);
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken) => base.StopAsync(cancellationToken);
}
