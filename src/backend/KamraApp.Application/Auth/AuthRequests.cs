using System.ComponentModel.DataAnnotations;

namespace KamraApp.Application.Auth;

// ADR-0006: password 15-128 characters, no composition rules (NIST SP 800-63B-4).
// Plain classes, not positional records: MVC rejects validation attributes on record properties.
public sealed class RegisterRequest
{
    [Required(ErrorMessage = "Add meg az e-mail-címed.")]
    [EmailAddress(ErrorMessage = "Adj meg egy érvényes e-mail-címet, például: nev@pelda.hu.")]
    [StringLength(256, ErrorMessage = "Az e-mail-cím legfeljebb 256 karakter lehet.")]
    public string? Email { get; init; }

    [Required(ErrorMessage = "Add meg a jelszavad.")]
    [StringLength(128, MinimumLength = 15, ErrorMessage = "A jelszó legalább 15 és legfeljebb 128 karakter legyen. Egy hosszabb, könnyen megjegyezhető mondat is jó.")]
    public string? Password { get; init; }
}

// Login does not reveal the password policy; wrong values get the uniform INVALID_CREDENTIALS answer.
public sealed class LoginRequest
{
    [Required(ErrorMessage = "Add meg az e-mail-címed.")]
    public string? Email { get; init; }

    [Required(ErrorMessage = "Add meg a jelszavad.")]
    public string? Password { get; init; }
}
