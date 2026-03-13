using FluentValidation;

namespace Demif.Application.Features.Lessons.Admin;

/// <summary>
/// Validator cho thao tác cập nhật Metadata (thông tin cơ bản) của Lesson.
/// </summary>
public class UpdateLessonMetadataValidator : AbstractValidator<UpdateLessonMetadataRequest>
{
    public UpdateLessonMetadataValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title không được để trống.")
            .MaximumLength(200).WithMessage("Title không được vượt quá 200 ký tự.");

        // AudioUrl hoặc MediaUrl phải có ít nhất một URL hợp lệ
        RuleFor(x => x.AudioUrl)
            .NotEmpty().WithMessage("AudioUrl không được để trống.")
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
            .WithMessage("AudioUrl phải là URL hợp lệ.")
            .When(x => string.IsNullOrWhiteSpace(x.MediaUrl));

        RuleFor(x => x.MediaUrl)
            .Must(url => url == null || Uri.TryCreate(url, UriKind.Absolute, out _))
            .WithMessage("MediaUrl phải là URL hợp lệ.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description không được vượt quá 1000 ký tự.");
    }
}
