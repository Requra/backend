using Requra.Domain.Enums;

namespace Requra.Domain.Entities
{
    public class MeetingSession
    {
        public Guid Id { get; private set; }

        public string? SessionToken { get; private set; }

        public string HostId { get; private set; } = null!;

        public DateTime? StartedAt { get; private set; }

        public DateTime? EndedAt { get; private set; }

        public MeetingStatus Status { get; private set; }

        public DateTime CreatedAt { get; private set; }

        public string? PlatformUrl { get; private set; }

        // Navigation
        public ApplicationUser Host { get; private set; } = null!;
        public ICollection<Document> Documents { get; private set; } = new List<Document>();

        // Constructor
        public MeetingSession(string hostId, string? sessionToken = null)
        {
            Id = Guid.NewGuid();
            HostId = hostId;
            SessionToken = sessionToken;

            Status = MeetingStatus.Scheduled;
            CreatedAt = DateTime.UtcNow;
        }

        public void Start()
        {
            Status = MeetingStatus.Live;
            StartedAt = DateTime.UtcNow;
        }

        public void End()
        {
            Status = MeetingStatus.Ended;
            EndedAt = DateTime.UtcNow;
        }

        public void Cancel()
        {
            Status = MeetingStatus.Canceled;
        }

        public void SetPlatform(string url)
        {
            PlatformUrl = url;
        }

        public void SetSessionToken(string token)
        {
            SessionToken = token;
        }
    }
}