using System.ComponentModel.DataAnnotations;

namespace ContactLandingApi.Requests;

public class CreateContactRequest
{
    [Required, MaxLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string Apellidos { get; set; } = string.Empty;

    [Required, MaxLength(30)]
    public string Telefono { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(150)]
    public string Correo { get; set; } = string.Empty;

    [MaxLength(150)]
    public string? Empresa { get; set; }

    public string? CaptchaToken { get; set; }
}
