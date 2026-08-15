using System.Text.Json.Serialization;

namespace Requra.Infrastructure.ExternalDTOs.ClickUpDto
{
    public class ClickUpTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = null!;

        [JsonPropertyName("token_type")]
        public string TokenType { get; set; } = null!;

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }
}
