global using static DbDemo.Constants;
global using DbDemo.Models;
global using DbDemo.Enums;
global using DbDemo.Extensions;
global using DbDemo.Helpers;
using DbDemo.Services;
using DbDemo.Workers;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

builder.Services.AddHostedService<DirectoryChecker>();
builder.Services.AddHostedService<LoopWorker>();
builder.Services.AddHostedService<AppWorker>();
builder.Services.AddSingleton<CacheService>();

builder.Services.AddSingleton<CryptoService>();
builder.Services.AddSingleton<DocTokenService>();

builder.Services.AddSingleton<IRedisDb, RedisDbRepo>();
builder.Services.AddSingleton<IMongoDb, MongoDbRepo>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.WebHost.ConfigureKestrel(x =>
{
    var port = int.Parse(config["Port"]);
    x.ListenAnyIP(port);
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthorization();

app.MapControllers();

await app.RunAsync();
