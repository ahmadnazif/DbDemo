using DbDemo.Services;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

namespace DbDemo.Controllers;

[Route("api/mongo")]
[ApiController]
public class MongoController(IMongoDb mongo) : ControllerBase
{
    private readonly IMongoDb mongo = mongo;
    private readonly Random r = new();

    [HttpPost("insert-random")]
    public async Task<ActionResult<ResponseBase>> InsertRandom()
    {
        var num = $"60{r.Next(100000000, 199999999)}";
        var oper = (char)('A' + r.Next(0, 26));

        return await mongo.SetAsync(num, oper.ToString(), DateTime.Now);
    }

    [HttpGet("get")]
    public async Task<ActionResult<PhoneNumber>> Get([FromQuery] string msisdn) => await mongo.GetAsync(msisdn);

    [HttpGet("count")]
    public async Task<ActionResult<long>> CountRow([FromQuery] int index) => await mongo.CountRowAsync(index);

}
