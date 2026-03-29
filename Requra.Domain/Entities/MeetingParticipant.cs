using Requra.Domain.Enums;

namespace Requra.Domain.Entities
{
    public class MeetingParticipant
    {
        public string UserId { get; private set; } = null!;

        public Guid MeetingId { get; private set; }

        public MeetingRole Role { get; private set; }

        public DateTime JoinedAt { get; private set; }

        // Navigation
        public ApplicationUser User { get; private set; } = null!;
        public MeetingSession Meeting { get; private set; } = null!;


        // Constructor
        private MeetingParticipant()
        {
            
        }
        public MeetingParticipant(string userId, Guid meetingId, MeetingRole role)
        {
            UserId = userId;
            MeetingId = meetingId;
            Role = role;
            JoinedAt = DateTime.UtcNow;
        }
    }
}