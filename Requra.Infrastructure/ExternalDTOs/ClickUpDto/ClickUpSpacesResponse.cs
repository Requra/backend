using System.Text.Json.Serialization;

namespace Requra.Infrastructure.ExternalDTOs.ClickUpDto
{
    public class ClickUpSpacesResponse
    {
        [JsonPropertyName("spaces")]
        public List<ClickUpSpaceDetail> Spaces { get; set; } = new();
    }

    public class ClickUpSpaceDetail
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = null!;

        [JsonPropertyName("name")]
        public string Name { get; set; } = null!;

        [JsonPropertyName("private")]
        public bool? Private { get; set; }

        [JsonPropertyName("color")]
        public string? Color { get; set; }

        [JsonPropertyName("avatar")]
        public string? Avatar { get; set; }
    }
}
