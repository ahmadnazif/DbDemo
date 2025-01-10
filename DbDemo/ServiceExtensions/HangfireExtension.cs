using Hangfire;
using Hangfire.Dashboard;
using Hangfire.Storage.MySql;
using System.Diagnostics.CodeAnalysis;
using System.Transactions;

namespace DbDemo.ServiceExtensions;

public static class HangfireExtension
{
    public static IServiceCollection AddHangfireService(this IServiceCollection service, ConfigurationManager config)
    {
        var mysqlConnectionString = GenerateConnectionString(config);
        service.AddHangfire(x =>
        {
            x.UseStorage(new MySqlStorage(mysqlConnectionString, new MySqlStorageOptions
            {
                TransactionIsolationLevel = IsolationLevel.ReadCommitted,
                QueuePollInterval = TimeSpan.FromSeconds(15),
                JobExpirationCheckInterval = TimeSpan.FromHours(1),
                CountersAggregateInterval = TimeSpan.FromMinutes(5),
                PrepareSchemaIfNecessary = true,
                DashboardJobListLimit = 50000,
                TransactionTimeout = TimeSpan.FromMinutes(1),
                TablesPrefix = "__Hangfire"
            }));
        });

        service.AddHangfireServer();

        return service;
    }

    private static string GenerateConnectionString(IConfiguration config)
    {
        var server = config["Db:Server"];
        var dbName = config["Db:DbName"];
        var userId = config["Db:UserId"];
        var password = config["Db:Password"];

        return $"Server={server};Database={dbName};User={userId};Password={password};";
    }

    public static IApplicationBuilder UseHangfireUI(this IApplicationBuilder app, string dashboardUrl)
    {
        return app.UseHangfireDashboard(dashboardUrl, new DashboardOptions
        {
            DisplayStorageConnectionString = true,
            DashboardTitle = "Hangfire Dashboard",
            Authorization = [new AuthFilter()]
        });
    }

    public class AuthFilter : IDashboardAuthorizationFilter
    {
        public AuthFilter() { }

        public bool Authorize([NotNull] DashboardContext context) => IsAuthorized(context.GetHttpContext(), false, 30);

        private bool IsAuthorized(HttpContext httpContext, bool useEncryption, int expiredMinute) => true;
    }
}
