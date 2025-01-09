using DbDemo.Models;
using StackExchange.Redis;
using System.Diagnostics;

namespace DbDemo.Services;

public class RedisDbRepo : IRedisDb
{
    private readonly ILogger<RedisDbRepo> logger;
    private readonly Lazy<Task<IConnectionMultiplexer>> connection;
    private readonly string hostAndPort;

    public RedisDbRepo(ILogger<RedisDbRepo> logger, IConfiguration config)
    {
        this.logger = logger;

        var host = config["RedisDb:Host"];
        var port = int.Parse(config["RedisDb:Port"]);
        var username = config["RedisDb:Username"];
        var password = config["RedisDb:Password"];
        hostAndPort = $"{host}:{port}";

        var configOption = new ConfigurationOptions
        {
            EndPoints = { { host, port }, },
            AllowAdmin = true,
            ClientName = "Redis Client",
            User = username,
            Password = password,
            ReconnectRetryPolicy = new LinearRetry(5000),
            AbortOnConnectFail = false,
        };

        connection = new Lazy<Task<IConnectionMultiplexer>>(async () =>
        {
            var mutex = await ConnectionMultiplexer.ConnectAsync(configOption);
            mutex.ErrorMessage += (a, b) => { this.logger.LogError($"{a}, Error: {b.Message}"); };
            mutex.InternalError += (a, b) => { this.logger.LogError($"{a}, Error: {b.Exception}"); };
            mutex.ConnectionFailed += (a, b) => { this.logger.LogError($"{a}, Error: {b.Exception}"); };
            mutex.ConnectionRestored += (a, b) => { this.logger.LogInformation($"{a}, Connection restored"); };
            return mutex;
        });
    }

    private async Task<IServer> GetServerAsync()
    {
        var val = await connection.Value;
        return val.GetServer(hostAndPort);
    }

    private async Task<IDatabase> GetDbAsync()
    {
        var val = await connection.Value;
        return val.GetDatabase();
    }

    public async Task<object> GetServerInfoAsync()
    {
        var server = await GetServerAsync();

        var serverTime = await server.TimeAsync();

        return new
        {
            server.DatabaseCount,
            server.Features,
            Ping = await server.PingAsync(),
            ServerType = server.ServerType.ToString(),
            ServerTimeUtc = serverTime,
            ServerTimeLocal = serverTime.ToLocalTime(),
            server.Version
        };
    }

    public async Task<List<object>> GetConnectedClientsAsync()
    {
        var server = await GetServerAsync();
        var clt = await server.ClientListAsync();
        List<object> objs = [];

        foreach(var c in clt)
        {
            objs.Add(new
            {
              c.Host,
              c.Port,
              c.Name,
              c.ProtocolVersion,
              c.LibraryVersion,
              c.AgeSeconds,
              c.LastCommand,
              c.LibraryName
            });
        }

        return objs;
    }

    public async Task<RedisType> GetKeyTypeAsync(string key)
    {
        var db = await GetDbAsync();
        return await db.KeyTypeAsync(key);
    }

    public async Task<ResponseBase> InsertUserAsync(User value)
    {
        try
        {
            Stopwatch sw = Stopwatch.StartNew();

            HashEntry[] entries =
            [
                new(nameof(value.Username), value.Username),
                new(nameof(value.Email), value.Email),
                new(nameof(value.Age), value.Age),
            ];

            var db = await GetDbAsync();

            await db.HashSetAsync(value.Username, entries);
            sw.Stop();

            return new ResponseBase
            {
                IsSuccess = true,
                Message = $"User '{value.Username}' set [{sw.Elapsed}]"
            };
        }
        catch (Exception ex)
        {
            return new ResponseBase
            {
                IsSuccess = false,
                Message = $"Exception: {ex.Message}"
            };
        }
    }

    public async Task<User> GetUserAsync(string key)
    {
        var db = await GetDbAsync();

        var all = await db.HashGetAllAsync(key);
        var dictionary = all.ToDictionary(entry => (string)entry.Name, entry => (string)entry.Value);

        return new()
        {
            Username = dictionary.TryGetValue(nameof(User.Username), out var username) ? username : null,
            Age = dictionary.TryGetValue(nameof(User.Age), out var ageStr) && int.TryParse(ageStr, out var age) ? age : 0,
            Email = dictionary.TryGetValue(nameof(User.Email), out var email) ? email : null
        };
    }

    public async Task<ResponseBase> InsertAsync<T>(string key, T value)
    {
        try
        {
            Stopwatch sw = Stopwatch.StartNew();

            var properties = typeof(T).GetProperties();
            HashEntry[] entries = new HashEntry[properties.Length];

            for (int i = 0; i < properties.Length; i++)
            {
                var property = properties[i];
                var propertyName = property.Name;
                var propertyValue = property.GetValue(value)?.ToString() ?? string.Empty; // Convert property value to string or use empty string for null
                entries[i] = new HashEntry(propertyName, propertyValue);
            }

            var db = await GetDbAsync();

            await db.HashSetAsync(key, entries);
            sw.Stop();

            return new ResponseBase
            {
                IsSuccess = true,
                Message = $"KEY {key} set [{sw.Elapsed}]"
            };
        }
        catch (Exception ex)
        {
            return new ResponseBase
            {
                IsSuccess = false,
                Message = $"Exception: {ex.Message}"
            };
        }
    }

    public async Task<string> GetStringValueAsync(string key, string fieldName)
    {
        var db = await GetDbAsync();

        await db.KeyTypeAsync(key);

        var val = await db.HashGetAsync(key, fieldName);

        if (!val.HasValue)
            return null;

        return val.ToString();
    }

    public async Task<long> CountDbRowAsync()
    {
        var server = await GetServerAsync();
        return await server.DatabaseSizeAsync();
    }

    public async Task<ResponseBase> DeleteAsync(string key)
    {
        try
        {
            Stopwatch sw = Stopwatch.StartNew();

            var db = await GetDbAsync();

            var succ = await db.KeyDeleteAsync(key);
            sw.Stop();

            return new ResponseBase
            {
                IsSuccess = succ,
                Message = succ ? $"Key '{key}' deleted [{sw.Elapsed}]" : $"Key '{key}' not found [{sw.Elapsed}]"
            };
        }
        catch (Exception ex)
        {
            return new ResponseBase
            {
                IsSuccess = false,
                Message = $"Exception: {ex.Message}"
            };
        }
    }

}

public interface IRedisDb
{
    Task<object> GetServerInfoAsync();
    Task<List<object>> GetConnectedClientsAsync();
    Task<RedisType> GetKeyTypeAsync(string key);
    Task<ResponseBase> InsertUserAsync(User value);
    Task<User> GetUserAsync(string key);
    Task<ResponseBase> InsertAsync<T>(string key, T value);
    Task<string> GetStringValueAsync(string key, string fieldName);
    Task<long> CountDbRowAsync();
    Task<ResponseBase> DeleteAsync(string key);
}
