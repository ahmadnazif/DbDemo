using DbDemo.Models;
using DbDemo.Services;
using Microsoft.AspNetCore.Mvc;

namespace DbDemo.Controllers;

[Route("api/redis")]
[ApiController]
public class RedisController(IRedisService redis) : ControllerBase
{
    private readonly IRedisService redis = redis;
    private readonly Random r = new();

    [HttpGet("get-server-info")]
    public async Task<ActionResult<object>> GetServerInfo() => await redis.GetServerInfoAsync();

    [HttpGet("get-connected-clients")]
    public async Task<ActionResult<List<object>>> GetConnectedClients() => await redis.GetConnectedClientsAsync();

    [HttpPost("insert-user")]
    public async Task<ActionResult<ResponseBase>> InsertUser([FromBody] User req) => await redis.InsertUserAsync(req);

    [HttpPost("insert-random-user")]
    public async Task<ActionResult<ResponseBase>> InsertRandomUser()
    {
        var id = Guid.NewGuid().ToString("N").ToUpper();
        User user = new()
        {
            Username = id,
            Age = r.Next(10, 30),
            Email = $"{id}@email.com"
        };

        return await redis.InsertUserAsync(user);
    }

    [HttpGet("get-user")]
    public async Task<ActionResult<User>> GetUser([FromQuery] string key) => await redis.GetUserAsync(key);

    [HttpGet("count")]
    public async Task<ActionResult<long>> CountDbRow() => await redis.CountDbRowAsync();

    [HttpGet("get-key-type")]
    public async Task<ActionResult<string>> GetKeyType([FromQuery] string key)
    {
        var kt = await redis.GetKeyTypeAsync(key);
        return kt.ToString();
    }

    [HttpGet("get-string-value")]
    public async Task<ActionResult<string>> GetStringValue([FromQuery] string key, [FromQuery] string fieldName) => await redis.GetStringValueAsync(key, fieldName);

    [HttpDelete("delete")]
    public async Task<ActionResult<ResponseBase>> Delete([FromQuery] string key) => await redis.DeleteAsync(key);

}
