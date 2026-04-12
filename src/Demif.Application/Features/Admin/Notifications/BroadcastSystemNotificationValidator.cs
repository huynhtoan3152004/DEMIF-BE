using FluentValidation;

namespace Demif.Application.Features.Admin.Notifications;

public class BroadcastSystemNotificationValidator : AbstractValidator<BroadcastSystemNotificationRequest>
{
    public BroadcastSystemNotificationValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(120);

        RuleFor(x => x.Message)
            .NotEmpty()
            .MaximumLength(4000);

        RuleFor(x => x.ActionUrl)
            .MaximumLength(500)
            .Must(BeValidUrl)
            .When(x => !string.IsNullOrWhiteSpace(x.ActionUrl));
    }

    private static bool BeValidUrl(string? url)
    {
        return string.IsNullOrWhiteSpace(url) || Uri.IsWellFormedUriString(url, UriKind.RelativeOrAbsolute);
    }
}