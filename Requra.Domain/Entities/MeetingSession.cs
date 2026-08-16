using Requra.Domain.Enums;

namespace Requra.Domain.Entities
{
    public class MeetingSession
    {

        public Guid Id { get; private set; }

        public string? SessionToken { get; private set; }
        public string? Title { get; private set; }
        public string? Description { get; private set; }
        public string HostId { get; private set; } = null!;
        public string CreatedById { get; private set; } = null!;
        public DateTime? StartedAt { get; private set; }
        public DateTime? EndedAt { get; private set; }
        public DateTime? ScheduledAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }
        public MeetingStatus Status { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public string? PlatformUrl { get; private set; }
        public int? DurationMinutes { get; private set; }
        public Guid ProjectId { get; private set; }
        public TranscriptStatus TranscriptStatus { get; private set; }
        public string? TranscriptDocumentUrl { get; private set; }
        public List<string> RecordingUrls { get; private set; } = new();
        public DateTime? MeetingEndsAt { get; private set; }

        // Navigation
        public ApplicationUser Host { get; private set; } = null!;
        public ApplicationUser CreatedBy { get; private set; } = null!;
        public Project Project { get; private set; } = null!;
        public ICollection<Document> Documents { get; private set; } = new List<Document>();
        public ICollection<MeetingParticipant> Participants { get; private set; } = new List<MeetingParticipant>();
        public ICollection<Recording> Recordings { get; private set; } = new List<Recording>();
        public ICollection<Invitation> Invitations { get; private set; } = new List<Invitation>();

        // Constructor

        private MeetingSession()
        {

        }
        public MeetingSession(Guid projectId, string hostId, string createdById, DateTime? scheduledAt = null, string? title = null, string? description = null, string? sessionToken = null)
        {
            Id = Guid.NewGuid();

            ProjectId = projectId;
            HostId = hostId;
            CreatedById = createdById;

            Title = title;
            Description = description;
            SessionToken = sessionToken;
            ScheduledAt = scheduledAt;

            Status = MeetingStatus.Scheduled;
            TranscriptStatus = TranscriptStatus.Pending;

            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }
        public MeetingSession(string? sessionToken = null)
        {
            Id = Guid.NewGuid();
            SessionToken = sessionToken;

            Status = MeetingStatus.Scheduled;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        //public void Start()
        //{
        //    Status = MeetingStatus.Live;
        //    StartedAt = DateTime.UtcNow;
        //    UpdatedAt = DateTime.UtcNow;
        //}
        public void Start(int mvpMaxLiveDurationMinutes=60)
        {
            Status = MeetingStatus.Live;
            StartedAt = DateTime.UtcNow;
            MeetingEndsAt = StartedAt.Value.AddMinutes(mvpMaxLiveDurationMinutes);
            UpdatedAt = DateTime.UtcNow;
        }

        public void End()
        {
            Status = MeetingStatus.Ended;
            UpdatedAt = DateTime.UtcNow;
            EndedAt = DateTime.UtcNow;

            if (StartedAt.HasValue)
            {
                DurationMinutes =
                    (int)(EndedAt.Value - StartedAt.Value).TotalMinutes;
            }
        }

        public void Cancel()
        {
            Status = MeetingStatus.Cancelled;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetPlatform(string url)
        {
            PlatformUrl = url;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetSessionToken(string token)
        {
            SessionToken = token;
            UpdatedAt = DateTime.UtcNow;
        }
        public void UpdateDetails(string? title,string? description,DateTime? scheduledAt)
        {
            if (title != null)
                Title = title;

            if (description != null)
                Description = description;

            if (scheduledAt.HasValue)
                ScheduledAt = scheduledAt.Value;

            UpdatedAt = DateTime.UtcNow;
        }
        //public void SetRecordingUrl(string url)
        //{
        //    RecordingUrl = url;
        //    UpdatedAt = DateTime.UtcNow;
        //}

        public void AddRecordingUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return;

            var normalizedUrl = url.Trim();

            var current = RecordingUrls ?? new List<string>();

            if (current.Any(x => string.Equals(x, normalizedUrl, StringComparison.OrdinalIgnoreCase)))
                return;

            RecordingUrls = current
                .Append(normalizedUrl)
                .ToList();

            UpdatedAt = DateTime.UtcNow;
        }
        public void SetRecordingUrls(IEnumerable<string> urls)
        {
            if (urls == null)
                return;

            RecordingUrls = urls
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            UpdatedAt = DateTime.UtcNow;
        }
    }
}