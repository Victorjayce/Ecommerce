using Ecommerce.Dtos;
using System.Text.Json.Serialization;

namespace Ecommerce.Models
{
    public class PaystackWebhookEvent
    {
        [JsonPropertyName("event")]
        public string? Event { get; set; }

        [JsonPropertyName("data")]
        public PaystackTransaction? Data { get; set; }
    }
}
