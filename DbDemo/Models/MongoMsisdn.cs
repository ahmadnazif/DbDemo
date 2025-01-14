using MongoDB.Bson.Serialization.Attributes;

namespace DbDemo.Models;

public class MongoMsisdn
{
    [BsonId]
    public string M { get; set; }
    public string O { get; set; }
    public DateTime U { get; set; }
}
