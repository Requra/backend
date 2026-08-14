namespace Requra.Infrastructure.ExternalDTOs.ClickUpDto
{
    public class ClickUpOAuthSettings
    {
        public string ClientId { get; set; } = null!;
        public string ClientSecret { get; set; } = null!;
        public string? RedirectUri { get; set; }
    }
}
