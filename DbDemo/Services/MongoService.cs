using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace DbDemo.Services;

public class MongoService : IMongoService
{
    private readonly ILogger<MongoService> consoleLogger;
    private readonly Dictionary<string, IMongoCollection<MongoUser>> collections = [];

    public MongoService(ILogger<MongoService> consoleLogger, IConfiguration config)
    {
        this.consoleLogger = consoleLogger;

        var host = config["MongoDb:Host"];
        var port = int.Parse(config["MongoDb:Port"]);
        var dbName = config["MongoDb:DbName"];
        var conString = $"mongodb://{host}:{port}";

        MongoClient client = new(conString);
        var db = client.GetDatabase(dbName);

        for (int i = 0; i < 10; i++)
        {
            collections.Add(i.ToString(), db.GetCollection<MongoUser>($"pl{i}"));
        }
    }
}

internal class MongoUser
{
    [BsonId]
    public string MS { get; set; }
    public string OP { get; set; }
    public DateTime UT { get; set; }
    public DateTime CT { get; set; }
}

public interface IMongoService
{

}
