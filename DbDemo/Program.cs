using DbDemo.Services;
using DbDemo.Workers;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

builder.Services.AddHostedService<AppWorker>();
builder.Services.AddSingleton<CacheService>();
builder.Services.AddSingleton<IRedisService, RedisService>();

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
