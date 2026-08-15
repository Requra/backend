using System.Text.Json.Serialization;

namespace Requra.Infrastructure.ExternalDTOs.ClickUpDto
{
    public class ClickUpTask
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = null!;

        [JsonPropertyName("name")]
        public string Name { get; set; } = null!;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("status")]
        public ClickUpTaskStatus? Status { get; set; }

        [JsonPropertyName("priority")]
        public ClickUpPriority? Priority { get; set; }

        [JsonPropertyName("list")]
        public ClickUpList? List { get; set; }

        [JsonPropertyName("folder")]
        public ClickUpFolder? Folder { get; set; }

        [JsonPropertyName("space")]
        public ClickUpSpace? Space { get; set; }

        [JsonPropertyName("custom_fields")]
        public List<ClickUpCustomField>? CustomFields { get; set; }

        [JsonPropertyName("date_created")]
        public string? DateCreated { get; set; }

        [JsonPropertyName("date_updated")]
        public string? DateUpdated { get; set; }

        [JsonPropertyName("creator")]
        public ClickUpUser? Creator { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }
    }

    public class ClickUpTaskStatus
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = null!;

        [JsonPropertyName("color")]
        public string? Color { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }
    }

    public class ClickUpPriority
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = null!;

        [JsonPropertyName("priority")]
        public string Priority { get; set; } = null!;

        [JsonPropertyName("color")]
        public string? Color { get; set; }
    }

    public class ClickUpList
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = null!;

        [JsonPropertyName("name")]
        public string Name { get; set; } = null!;
    }

    public class ClickUpFolder
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = null!;

        [JsonPropertyName("name")]
        public string Name { get; set; } = null!;
    }

    public class ClickUpSpace
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = null!;

        [JsonPropertyName("name")]
        public string Name { get; set; } = null!;
    }

    public class ClickUpUser
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("username")]
        public string Username { get; set; } = null!;

        [JsonPropertyName("email")]
        public string? Email { get; set; }
    }

    public class ClickUpCustomField
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = null!;

        [JsonPropertyName("value")]
        public object? Value { get; set; }
    }
}
