namespace Requra.Infrastructure.ExternalDTOs.ClickUpDto
{
    public class ClickUpOAuthSettings
    {
        public string ClientId { get; set; } = null!;
        public string ClientSecret { get; set; } = null!;
        public CallbackUrlSettings CallbackUrls { get; set; } = new();
    }

    public class CallbackUrlSettings
    {
        public string Web { get; set; } = "https://requra-ai.vercel.app/integrations/clickup/callback";
        public string Mobile { get; set; } = "requra://clickup/callback";
    }
}
