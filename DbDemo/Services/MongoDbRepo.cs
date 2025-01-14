using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System.Runtime.CompilerServices;

namespace DbDemo.Services;

public class MongoDbRepo : IMongoDb
{
    private readonly Dictionary<string, IMongoCollection<MongoMsisdn>> collections = [];

    public MongoDbRepo(IConfiguration config)
    {
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
            collections.Add(collectionName, db.GetCollection<MongoMsisdn>(collectionName));
        }
    }

    private static string GetCollectionName(int index) => $"p{index}";

    private IMongoCollection<MongoMsisdn> GetCollectionByMsisdn(string msisdn)
    {
        if (string.IsNullOrWhiteSpace(msisdn))
            return null;

        var end = int.Parse(msisdn[^1].ToString());
        var key = GetCollectionName(end);
        return collections[key];
    }

    private IMongoCollection<MongoMsisdn> GetCollectionByCollectionName(string collectionName)
    {
        var exist = collections.TryGetValue(collectionName, out var result);
        return exist ? result : null;
    }

    private static ResponseBase UnknownCollection() => new() { IsSuccess = false, Message = $"Unknown collection" };

    private static FilterDefinition<MongoMsisdn> EqFilter(string msisdn) => Builders<MongoMsisdn>.Filter.Eq(x => x.M, msisdn);

    public async Task<long> CountAsync(string collectionName)
    {
        var coll = GetCollectionByCollectionName(collectionName);
        return coll == null ? 0 : await coll.EstimatedDocumentCountAsync(); //.CountDocumentsAsync(new BsonDocument());
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

        var coll = GetCollectionByMsisdn(msisdn);
        if (coll == null)
            return UnknownCollection();

        var result = await coll.DeleteOneAsync(filter);

        return new ResponseBase
        {
            IsSuccess = result.IsAcknowledged,
            Message = result.IsAcknowledged ? $"{result.DeletedCount} item deleted" : $"Result is not acknowledged"
        };
    }

    public async Task<Msisdn> GetAsync(string msisdn)
    {
        var filter = EqFilter(msisdn);

        var coll = GetCollectionByMsisdn(msisdn);
        if (coll == null)
            return null;

        var result = await coll.Find(filter).FirstOrDefaultAsync();

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

        var coll = GetCollectionByMsisdn(msisdn);
        if (coll == null)
            return null;

        var result = await coll.Find(filter).FirstOrDefaultAsync();

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

        var coll = GetCollectionByMsisdn(msisdn);
        if (coll == null)
            return false;

        return await coll.Find(filter).AnyAsync();
    }

    public async Task<ResponseBase> SetAsync(string msisdn, string @operator, DateTime? updateTime = null)
    {
        var filter = EqFilter(msisdn);

        var coll = GetCollectionByMsisdn(msisdn);
        if (coll == null)
            return UnknownCollection();

        if (await IsExistAsync(msisdn))
        {
            var ut = updateTime ?? DateTime.Now;

            var updateData = Builders<MongoMsisdn>.Update
                .Set(x => x.O, @operator)
                .Set(x => x.U, ut);

            var result = await coll.UpdateOneAsync(filter, updateData);
            return new()
            {
                IsSuccess = result.IsAcknowledged,
                Message = result.IsAcknowledged ? $"Updated = {msisdn}, {@operator}, {ut}" : null
            };
        }
        else
        {
            MongoMsisdn num = new()
            {
                M = msisdn,
                O = @operator,
                U = updateTime ?? DateTime.Now,
            };

            await coll.InsertOneAsync(num);
            return new()
            {
                IsSuccess = true,
                Message = $"Created = {num.M}, {num.O}, {num.U}"
            };
        }
    }

    public async IAsyncEnumerable<Msisdn> StreamAsync(string collectionName, [EnumeratorCancellation] CancellationToken ct)
    {
        var filter = Builders<MongoMsisdn>.Filter.Empty;

        var coll = GetCollectionByCollectionName(collectionName);
        if (coll == null)
            yield break;

        var list = await coll.Find(filter).ToListAsync(cancellationToken: ct);

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
}

public interface IMongoDb : IPhoneLibraryDb
{
    Task<Dictionary<string, long>> GetCollectionCountAsDictionaryAsync();
    Task<long> CountAsync(string collctionName);
    IAsyncEnumerable<Msisdn> StreamAsync(string collectionName, CancellationToken ct);
}
