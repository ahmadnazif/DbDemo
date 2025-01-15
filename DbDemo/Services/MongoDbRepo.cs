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

    private const string RESULT_NOT_ACK = "Result is not acknowledged";
    private static ResponseBase UnknownCollection() => new() { IsSuccess = false, Message = $"Unknown collection" };

    private static string GetCollectionName(int index) => $"p{index}";

    private IMongoCollection<MongoMsisdn>? GetCollectionByMsisdn(string msisdn)
    {
        if (string.IsNullOrWhiteSpace(msisdn))
            return null;

        var end = int.Parse(msisdn[^1].ToString());
        var key = GetCollectionName(end);
        return collections[key];
    }

    private IMongoCollection<MongoMsisdn>? GetCollectionByCollectionName(string collectionName)
    {
        var exist = collections.TryGetValue(collectionName, out var result);
        return exist ? result : null;
    }

    private static FilterDefinition<MongoMsisdn> EqFilter(string msisdn) => Builders<MongoMsisdn>.Filter.Eq(x => x.M, msisdn);

    private static FilterDefinition<T> EqFilter<T>(string field, string value) => Builders<T>.Filter.Eq(field, value);

    public IEnumerable<string> ListCollectionNames() => collections.Keys;

    public async Task<Dictionary<string, long>> GetCollectionsCountAsync()
    {
        var colls = await Task.WhenAll(collections.Select(async x => new
        {
            x.Key,
            Count = await x.Value.EstimatedDocumentCountAsync()
        }));

        return colls.ToDictionary(x => x.Key, y => y.Count);
    }

    public async Task<long> GetCollectionCountAsync(string collectionName)
    {
        var coll = GetCollectionByCollectionName(collectionName);
        return coll == null ? 0 : await coll.EstimatedDocumentCountAsync(); //.CountDocumentsAsync(new BsonDocument());
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
            Message = result.IsAcknowledged ? $"{result.DeletedCount} item deleted" : RESULT_NOT_ACK
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

    public async Task<ResponseBase> SetAsync(string msisdn, string @operator)
    {
        var filter = EqFilter(msisdn);

        var coll = GetCollectionByMsisdn(msisdn);
        if (coll == null)
            return UnknownCollection();

        var ut = DateTime.Now;

        if (await IsExistAsync(msisdn))
        {
            var updateData = Builders<MongoMsisdn>.Update
                .Set(x => x.O, @operator)
                .Set(x => x.U, ut);

            var result = await coll.UpdateOneAsync(filter, updateData);
            return new()
            {
                IsSuccess = result.IsAcknowledged,
                Message = result.IsAcknowledged ? $"Updated = {msisdn}, {@operator}, {ut}" : RESULT_NOT_ACK
            };
        }
        else
        {
            MongoMsisdn num = new()
            {
                M = msisdn,
                O = @operator,
                U = ut,
            };

            await coll.InsertOneAsync(num);
            return new()
            {
                IsSuccess = true,
                Message = $"Created = {num.M}, {num.O}, {num.U}"
            };
        }
    }

    public async IAsyncEnumerable<Msisdn> StreamAsync(string collectionName, int delayMs, [EnumeratorCancellation] CancellationToken ct)
    {
        var filter = Builders<MongoMsisdn>.Filter.Empty;

        var coll = GetCollectionByCollectionName(collectionName);
        if (coll == null)
            yield break;

        using var cursor = await coll.FindAsync(filter, cancellationToken: ct);

        while (await cursor.MoveNextAsync(ct))
        {
            foreach (var data in cursor.Current)
            {
                if (ct.IsCancellationRequested)
                    yield break;

                yield return new()
                {
                    Msisdn = data.M,
                    Operator = data.O,
                    UpdateTime = data.U
                };

                if (delayMs > 0)
                    await Task.Delay(delayMs, ct);
            }
        }
    }
}

public interface IMongoDb : IPhoneLibraryDb
{
    IEnumerable<string> ListCollectionNames();
    Task<Dictionary<string, long>> GetCollectionsCountAsync();
    Task<long> GetCollectionCountAsync(string collectionName);
    IAsyncEnumerable<Msisdn> StreamAsync(string collectionName, int delayMs, CancellationToken ct);
}
