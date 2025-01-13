using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System.Runtime.CompilerServices;

namespace DbDemo.Services;

public class MongoDbRepo : IMongoDb
{
    private readonly ILogger<MongoDbRepo> consoleLogger;
    private readonly Dictionary<string, IMongoCollection<MongoPhoneNumber>> collections = [];

    public MongoDbRepo(ILogger<MongoDbRepo> consoleLogger, IConfiguration config)
    {
        this.consoleLogger = consoleLogger;
        var useMongoUrl = bool.Parse(config["MongoDb:UseMongoUrl"]);

        MongoClient client;
        string dbName;

        if (useMongoUrl)
        {
            var prefix = "MongoDb:MongoUrl";
            var mcs = MongoClientSettings.FromConnectionString(config[$"{prefix}:ConnectionString"]);
            dbName = config[$"{prefix}:DbName"];
            client = new(mcs);
        }
        else
        {
            var prefix = "MongoDb:Custom";
            var host = config[$"{prefix}:Host"];
            var port = int.Parse(config[$"{prefix}:Port"]);
            dbName = config[$"{prefix}:DbName"];
            var conString = $"mongodb://{host}:{port}";
            client = new(conString);
        }

        var db = client.GetDatabase(dbName);

        for (int i = 0; i < 10; i++)
        {
            var collectionName = GetCollectionName(i);
            collections.Add(collectionName, db.GetCollection<MongoPhoneNumber>(collectionName));
        }
    }

    private static string GetCollectionName(int index) => $"p{index}";

    private IMongoCollection<MongoPhoneNumber> GetCollection(string msisdn)
    {
        var end = int.Parse(msisdn[^1].ToString());
        var key = GetCollectionName(end);
        return collections[key];
    }

    private IMongoCollection<MongoPhoneNumber> GetCollection(int index)
    {
        var key = GetCollectionName(index);
        return collections[key];
    }

    private static FilterDefinition<MongoPhoneNumber> EqFilter(string msisdn) => Builders<MongoPhoneNumber>.Filter.Eq(x => x.M, msisdn);

    public async Task<long> CountRowAsync(int? index = null)
    {
        if (index < 0 || index > 10)
            return 0;

        return await GetCollection(index ?? 0).EstimatedDocumentCountAsync(); //.CountDocumentsAsync(new BsonDocument());
    }

    public async Task<Dictionary<string, long>> GetCollectionCountAsDictionaryAsync()
    {
        Dictionary<string, long> cols = [];
        foreach (var c in collections)
        {
            cols.Add(c.Key, await c.Value.EstimatedDocumentCountAsync());
        }

        return cols;

    }

    public async Task<ResponseBase> DeleteAsync(string msisdn)
    {
        var filter = EqFilter(msisdn);
        var result = await GetCollection(msisdn).DeleteOneAsync(filter);

        return new ResponseBase
        {
            IsSuccess = result.IsAcknowledged,
            Message = result.IsAcknowledged ? $"{result.DeletedCount} item deleted" : $"Result is not acknowledged"
        };
    }

    public async Task<PhoneNumber> GetAsync(string msisdn)
    {
        var filter = EqFilter(msisdn);
        var result = await GetCollection(msisdn).Find(filter).FirstOrDefaultAsync();

        if (result == null)
            return null;
        else
            return new()
            {
                Msisdn = result.M,
                Operator = result.O,
                UpdateTime = result.U,
            };
    }

    public async Task<string> GetAsync(string msisdn, MsisdnField field)
    {
        var filter = EqFilter(msisdn);
        var result = await GetCollection(msisdn).Find(filter).FirstOrDefaultAsync();

        if (result == null)
            return null;
        else
            return field switch
            {
                MsisdnField.Operator => result.O,
                MsisdnField.LastUpdatedDate => result.U.ToDbDateTimeString(),
                _ => null
            };
    }

    public async Task<bool> IsExistAsync(string msisdn)
    {
        var filter = EqFilter(msisdn);
        return await GetCollection(msisdn).Find(filter).AnyAsync();
    }

    public async Task<ResponseBase> SetAsync(string msisdn, string @operator, DateTime? updateTime = null)
    {
        var filter = EqFilter(msisdn);

        if (await IsExistAsync(msisdn))
        {
            var ut = updateTime ?? DateTime.Now;

            var updateData = Builders<MongoPhoneNumber>.Update
                .Set(x => x.O, @operator)
                .Set(x => x.U, ut);

            var result = await GetCollection(msisdn).UpdateOneAsync(filter, updateData);
            return new()
            {
                IsSuccess = result.IsAcknowledged,
                Message = result.IsAcknowledged ? $"Updated = {msisdn}, {@operator}, {ut}" : null
            };
        }
        else
        {
            MongoPhoneNumber num = new()
            {
                M = msisdn,
                O = @operator,
                U = updateTime ?? DateTime.Now,
            };

            await GetCollection(msisdn).InsertOneAsync(num);
            return new()
            {
                IsSuccess = true,
                Message = $"Created = {num.M}, {num.O}, {num.U}"
            };
        }
    }

    public async IAsyncEnumerable<PhoneNumber> StreamAsync(int? index, [EnumeratorCancellation] CancellationToken ct)
    {
        var filter = Builders<MongoPhoneNumber>.Filter.Empty;
        var list = await GetCollection(index ?? 0).Find(filter).ToListAsync(cancellationToken: ct);

        foreach (var l in list)
        {
            if (ct.IsCancellationRequested)
                yield break;

            yield return new()
            {
                Msisdn = l.M,
                Operator = l.O,
                UpdateTime = l.U
            };
        }
    }

    public Task<ResponseBase> SaveDocAsync(string username, string filename)
    {
        throw new NotImplementedException();
    }

    public Task<string> GetDocAsync(string username)
    {
        throw new NotImplementedException();
    }

    public Task<bool> IsUserExistAsync(string username)
    {
        throw new NotImplementedException();
    }
}

internal class MongoPhoneNumber
{
    [BsonId]
    public string M { get; set; }
    public string O { get; set; }
    public DateTime U { get; set; }
}

public interface IMongoDb : IMsisdnDb
{
    Task<Dictionary<string, long>> GetCollectionCountAsDictionaryAsync();
}
