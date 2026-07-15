
using FluentValidation;
using Requra.Application.DTOs.Meeting;

namespace Requra.Application.DTOValidations
{

    public class CreateMeetingRequestValidator : AbstractValidator<CreateMeetingRequest>
    {
        public CreateMeetingRequestValidator()
        {
            // Title (Required, 3–150 chars)
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Title is required.")
                .MinimumLength(3).WithMessage("Title must be at least 3 characters.")
                .MaximumLength(150).WithMessage("Title must not exceed 150 characters.");

            // Description (Optional, max 1000 chars)
            RuleFor(x => x.Description)
                .MaximumLength(1000)
                .WithMessage("Description must not exceed 1000 characters.")
                .When(x => !string.IsNullOrEmpty(x.Description));

            // ScheduledAt (Optional, must be future if provided)
            RuleFor(x => x.ScheduledAt)
                .Must(date => date == null || date > DateTime.UtcNow)
                .WithMessage("Scheduled date must be in the future.");
        }
    }
}
