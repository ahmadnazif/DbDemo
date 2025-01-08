using DbDemo.Services;
using Microsoft.AspNetCore.Mvc;

namespace DbDemo.Controllers;

[Route("api/app")]
[ApiController]
public class AppController(CacheService cache) : ControllerBase
{
    private readonly CacheService cache = cache;

    [HttpGet("get-start-time")]
    public ActionResult<DateTime> GetStartTime()
    {
        return cache.AppStartTime;
    }

    [HttpGet("get-duration-since-start-time")]
    public ActionResult<string> GetDurationSinceStartTime()
    {
        var dur = DateTime.Now.Subtract(cache.AppStartTime);
        return dur.ToString();
    }
}
