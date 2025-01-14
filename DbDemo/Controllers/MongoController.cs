using DbDemo.Services;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;
using System.Runtime.CompilerServices;

namespace DbDemo.Controllers;

[Route("api/mongo")]
[ApiController]
public class MongoController(IMongoDb mongo) : ControllerBase
{
    private readonly IMongoDb mongo = mongo;
    private readonly Random r = new();

    [HttpGet("get-collection-count-as-dictionary")]
    public async Task<ActionResult<Dictionary<string, long>>> GetColCountAsDict() => await mongo.GetCollectionCountAsDictionaryAsync();

    [HttpPost("insert-random")]
    public async Task<ActionResult<ResponseBase>> InsertRandom()
    {
        var num = $"60{r.Next(100000000, 199999999)}";
        var oper = (char)('A' + r.Next(0, 26));

        return await mongo.SetAsync(num, oper.ToString(), DateTime.Now);
    }

    [HttpGet("get")]
    public async Task<ActionResult<Msisdn>> Get([FromQuery] string msisdn) => await mongo.GetAsync(msisdn);

    [HttpDelete("delete")]
    public async Task<ActionResult<ResponseBase>> Delete([FromQuery] string msisdn) => await mongo.DeleteAsync(msisdn);

    [HttpGet("count")]
    public async Task<ActionResult<long>> CountRow([FromQuery] string collectionName) => await mongo.CountAsync(collectionName);

    [HttpGet("stream")]
    public IAsyncEnumerable<Msisdn> Stream([FromQuery] string collectionName, [FromQuery] int delayMs, CancellationToken ct) => mongo.StreamAsync(collectionName, delayMs, ct);

}
