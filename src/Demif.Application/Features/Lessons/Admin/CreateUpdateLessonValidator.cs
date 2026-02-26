using FluentValidation;

namespace Demif.Application.Features.Lessons.Admin;

/// <summary>
/// Validator cho CreateUpdateLessonRequest — ISSUE-01 fix
/// Đảm bảo dữ liệu hợp lệ trước khi tạo/cập nhật lesson
/// </summary>
public class CreateUpdateLessonValidator : AbstractValidator<CreateUpdateLessonRequest>
{
    private static readonly string[] AllowedStatuses = ["draft", "published", "archived"];

    public CreateUpdateLessonValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title không được để trống.")
            .MaximumLength(200).WithMessage("Title không được vượt quá 200 ký tự.");

        RuleFor(x => x.FullTranscript)
            .NotEmpty().WithMessage("FullTranscript không được để trống.")
            .MinimumLength(5).WithMessage("FullTranscript quá ngắn (tối thiểu 5 ký tự).");

        RuleFor(x => x.DurationSeconds)
            .GreaterThan(0).WithMessage("DurationSeconds phải lớn hơn 0.");

        // AudioUrl hoặc MediaUrl phải có ít nhất một URL hợp lệ
        RuleFor(x => x.AudioUrl)
            .NotEmpty().WithMessage("AudioUrl không được để trống.")
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
            .WithMessage("AudioUrl phải là URL hợp lệ.")
            .When(x => string.IsNullOrWhiteSpace(x.MediaUrl));

        RuleFor(x => x.MediaUrl)
            .Must(url => url == null || Uri.TryCreate(url, UriKind.Absolute, out _))
            .WithMessage("MediaUrl phải là URL hợp lệ.");

        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status không được để trống.")
            .Must(s => AllowedStatuses.Contains(s.ToLower()))
            .WithMessage("Status phải là một trong: draft, published, archived.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description không được vượt quá 1000 ký tự.");
    }
}
