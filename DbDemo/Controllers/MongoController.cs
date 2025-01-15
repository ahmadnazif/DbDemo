using DbDemo.Services;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;
using System.Runtime.CompilerServices;

namespace DbDemo.Controllers;

[Route("api/mongo")]
[ApiController]
public class MongoController(ILogger<MongoController> logger, IMongoDb mongo) : ControllerBase
{
    private readonly ILogger<MongoController> logger = logger;
    private readonly IMongoDb mongo = mongo;
    private readonly Random r = new();

    [HttpGet("list-collection-names")]
    public ActionResult<IEnumerable<string>> ListCollectionNames() => mongo.ListCollectionNames().ToList();


    [HttpGet("get-collections-count")]
    public async Task<ActionResult<Dictionary<string, long>>> GetCollectionsCount() => await mongo.GetCollectionsCountAsync();

    [HttpGet("get-collection-count")]
    public async Task<ActionResult<long>> CountRow([FromQuery] string collectionName) => await mongo.GetCollectionCountAsync(collectionName);

    [HttpPost("insert-random")]
    public async Task<ActionResult<ResponseBase>> InsertRandom()
    {
        var num = $"60{r.Next(100000000, 199999999)}";
        var oper = (char)('A' + r.Next(0, 26));

        return await mongo.SetAsync(num, oper.ToString());
    }

    [HttpGet("is-exist")]
    public async Task<ActionResult<bool>> IsExist([FromQuery] string msisdn) => await mongo.IsExistAsync(msisdn);

    [HttpGet("get")]
    public async Task<ActionResult<Msisdn>> Get([FromQuery] string msisdn) => await mongo.GetAsync(msisdn);

    [HttpDelete("delete")]
    public async Task<ActionResult<ResponseBase>> Delete([FromQuery] string msisdn) => await mongo.DeleteAsync(msisdn);

    [HttpGet("stream")]
    public IAsyncEnumerable<Msisdn> Stream([FromQuery] string collectionName, [FromQuery] int delayMs, CancellationToken ct) => mongo.StreamAsync(collectionName, delayMs, ct);

    [HttpGet("stream2")]
    public async IAsyncEnumerable<string> Stream2([FromQuery] string collectionName, [FromQuery] int delayMs, [EnumeratorCancellation] CancellationToken ct)
    {
        await foreach (var data in mongo.StreamAsync(collectionName, delayMs, ct))
        {
            if (ct.IsCancellationRequested)
                yield break;

            var str = $"{data.Msisdn}, {data.Operator}, {data.UpdateTime}";

            logger.LogInformation(str);
            yield return str;
        }
    }

    [HttpGet("demo-stream")]
    public async IAsyncEnumerable<int> DemoStream([FromQuery] int total, [FromQuery] int delayMs, [EnumeratorCancellation] CancellationToken ct)
    {
        var list = Enumerable.Range(1, total);
        foreach (var l in list)
        {
            if (ct.IsCancellationRequested)
                yield break;

            yield return l;
            logger.LogInformation(l.ToString());

            if (delayMs > 0)
                await Task.Delay(delayMs, ct);
        }

        //for (int i = 0; i < total; i++)
        //{
        //    if (ct.IsCancellationRequested)
        //        yield break;

        //    yield return i.ToString();
        //    logger.LogInformation(i.ToString());

        //    await Task.Delay(delayMs, ct);
        //}
    }
}
