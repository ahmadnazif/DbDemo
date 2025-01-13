using System.Text.Json.Serialization;

namespace DbDemo.Models;

public class Msisdn : MsisdnBase
{
    [JsonPropertyName(UPDATE_TIME)] public DateTime UpdateTime { get; set; }
}
