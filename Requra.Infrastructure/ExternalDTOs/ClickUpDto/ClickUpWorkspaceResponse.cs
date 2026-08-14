using System.Text.Json.Serialization;

namespace Requra.Infrastructure.ExternalDTOs.ClickUpDto
{
    public class ClickUpWorkspaceResponse
    {
        [JsonPropertyName("user")]
        public ClickUpUserInfo User { get; set; } = null!;
    }

    public class ClickUpUserInfo
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("username")]
        public string Username { get; set; } = null!;

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("color")]
        public string? Color { get; set; }

        [JsonPropertyName("profilePicture")]
        public string? ProfilePicture { get; set; }

        [JsonPropertyName("initials")]
        public string? Initials { get; set; }

        [JsonPropertyName("week_start_day")]
        public int? WeekStartDay { get; set; }

        [JsonPropertyName("timezone")]
        public string? Timezone { get; set; }
    }

    public class ClickUpTeamsResponse
    {
        [JsonPropertyName("teams")]
        public List<ClickUpTeam> Teams { get; set; } = new();
    }

    public class ClickUpTeam
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = null!;

        [JsonPropertyName("name")]
        public string Name { get; set; } = null!;

        [JsonPropertyName("spaces")]
        public List<ClickUpSpace>? Spaces { get; set; }
    }
}

