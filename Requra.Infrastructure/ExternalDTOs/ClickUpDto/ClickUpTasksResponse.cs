using System.Text.Json.Serialization;

namespace Requra.Infrastructure.ExternalDTOs.ClickUpDto
{
    public class ClickUpTasksResponse
    {
        [JsonPropertyName("tasks")]
        public List<ClickUpTask> Tasks { get; set; } = new();
    }
}
