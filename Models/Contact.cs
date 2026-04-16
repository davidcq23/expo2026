using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContactLandingApi.Models;

[Table("contacts")]
public class Contact
{
    public int Id { get; set; }

    [MaxLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [MaxLength(150)]
    public string Apellidos { get; set; } = string.Empty;

    [MaxLength(30)]
    public string Telefono { get; set; } = string.Empty;

    [MaxLength(150)]
    public string Correo { get; set; } = string.Empty;

    [MaxLength(150)]
    public string? Empresa { get; set; }

    [MaxLength(50)]
    public string Codigo { get; set; } = string.Empty;

    public bool UsoCodigo { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
