using DbDemo.Models;
using DbDemo.Services;
using Microsoft.AspNetCore.Mvc;

namespace DbDemo.Controllers;

[Route("api/redis")]
[ApiController]
public class RedisController(IRedisDb redis) : ControllerBase
{
    private readonly IRedisDb redis = redis;
    private readonly Random r = new();

    [HttpGet("get-server-info")]
    public async Task<ActionResult<object>> GetServerInfo() => await redis.GetServerInfoAsync();

    [HttpGet("get-connected-clients")]
    public async Task<ActionResult<List<object>>> GetConnectedClients() => await redis.GetConnectedClientsAsync();

    [HttpPost("insert-random")]
    public async Task<ActionResult<ResponseBase>> InsertRandom()
    {
        var num = $"60{r.Next(100000000, 199999999)}";
        var oper = (char)('A' + r.Next(0, 26));

        return await redis.SetAsync(num, oper.ToString(), DateTime.Now);
    }

    [HttpGet("get")]
    public async Task<ActionResult<Msisdn>> Get([FromQuery] string msisdn) => await redis.GetAsync(msisdn);

    [HttpGet("count")]
    public async Task<ActionResult<long>> CountDbRow() => await redis.CountRowAsync();

    [HttpGet("get-key-type")]
    public async Task<ActionResult<string>> GetKeyType([FromQuery] string key)
    {
        var kt = await redis.GetKeyTypeAsync(key);
        return kt.ToString();
    }

    [HttpDelete("delete")]
    public async Task<ActionResult<ResponseBase>> Delete([FromQuery] string key) => await redis.DeleteAsync(key);

}
