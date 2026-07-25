using Requra.Domain.Enums;

namespace Requra.Domain.Entities
{
    public class MeetingParticipant
    {
        public Guid Id { get; private set; }

        public Guid MeetingId { get; private set; }

        // Null for guests who join without an account.
        public string? UserId { get; private set; }

        public string? DisplayName { get; private set; }
        public string? Email { get; private set; }

        public MeetingRole Role { get; private set; }
        public ParticipantStatus Status { get; private set; }

        public bool RecordingConsent { get; private set; }
        public DateTime? ConsentedAt { get; private set; }

        public DateTime JoinedAt { get; private set; }
        public DateTime? LeftAt { get; private set; }

        // Navigation
        public ApplicationUser User { get; private set; } = null!;
        public MeetingSession Meeting { get; private set; } = null!;


        // Constructor
        private MeetingParticipant()
        {
            
        }
        //public MeetingParticipant(string userId, Guid meetingId, MeetingRole role)
        //{
        //    UserId = userId;
        //    MeetingId = meetingId;
        //    Role = role;
        //    JoinedAt = DateTime.UtcNow;
        //}
        public MeetingParticipant(
           Guid meetingId,
           string? userId,
           string? displayName,
           string? email,
           MeetingRole role)
        {
            Id = Guid.NewGuid();
            MeetingId = meetingId;
            UserId = userId;
            DisplayName = displayName;
            Email = email;
            Role = role;
            Status = ParticipantStatus.Joined;
            JoinedAt = DateTime.UtcNow;
        }
        public void MarkLeft()
        {
            Status = ParticipantStatus.Left;
            LeftAt = DateTime.UtcNow;
        }

        public void MarkRemoved()
        {
            Status = ParticipantStatus.Removed;
            LeftAt = DateTime.UtcNow;
        }

        // 3shan lw el participant 3ml left w 3ml rejoin, yb2a el status yb2a joined tany w el leftAt yb2a null
        public void Rejoin()
        {
            Status = ParticipantStatus.Joined;
            JoinedAt = DateTime.UtcNow;
            LeftAt = null;
        }

        public void SetConsent(bool consent)
        {
            RecordingConsent = consent;
            if (consent)
                ConsentedAt = DateTime.UtcNow;
        }
    }
}