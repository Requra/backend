using System.Text.Json.Serialization;

namespace Requra.Infrastructure.ExternalDTOs.ClickUpDto
{
    public class ClickUpListsResponse
    {
        [JsonPropertyName("lists")]
        public List<ClickUpList> Lists { get; set; } = new();
    }
}

