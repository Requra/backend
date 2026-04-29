using FluentValidation;
using Microsoft.AspNetCore.Http;
using Requra.Application.DTOs.Profile;

public class UploadAvatarDtoValidator : AbstractValidator<UploadAvatarDto>
{

    public UploadAvatarDtoValidator()
    {
        RuleFor(x => x.File)
            .NotNull().WithMessage("File is required.")
            .Must(BeAValidImageType).WithMessage("File type unsupported")
            .Must(file => file.Length <= 5 * 1024 * 1024).WithMessage("File size must be less than 5MB");

    }

    private bool BeAValidImageType(IFormFile file)
    {
        var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp" };

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };

        var extension = Path.GetExtension(file.FileName).ToLower();

        return allowedTypes.Contains(file.ContentType)
               && allowedExtensions.Contains(extension);
    }
}