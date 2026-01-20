using FluentValidation;

namespace Demif.Application.Features.Auth.FirebaseLogin;

/// <summary>
/// Validator cho FirebaseLoginRequest
/// </summary>
public class FirebaseLoginValidator : AbstractValidator<FirebaseLoginRequest>
{
    public FirebaseLoginValidator()
    {
        RuleFor(x => x.IdToken)
            .NotEmpty().WithMessage("Firebase ID Token không được để trống")
            .MinimumLength(100).WithMessage("Firebase ID Token không hợp lệ");
    }
}
