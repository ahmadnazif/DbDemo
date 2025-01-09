using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DbDemo.Models;

public class PhoneNumberBase
{
    [Required]
    [JsonPropertyName(MSISDN)]
    public string Msisdn { get; set; }

    [Required]
    [StringLength(1, ErrorMessage = "Only 1 character allowed for Operator")]
    [JsonPropertyName(OPERATOR)]
    public string Operator { get; set; }
}
