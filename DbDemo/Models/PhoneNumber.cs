using System.Text.Json.Serialization;

namespace DbDemo.Models;

public class PhoneNumber : PhoneNumberBase
{
    [JsonPropertyName(UPDATE_TIME)] public DateTime UpdateTime { get; set; }
    [JsonPropertyName(CREATED_TIME)] public DateTime CreatedTime { get; set; }
}
