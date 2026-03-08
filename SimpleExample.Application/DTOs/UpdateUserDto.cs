using System.ComponentModel.DataAnnotations;

namespace SimpleExample.Application.DTOs;

public class UpdateUserDto
{
    [Required(ErrorMessage = "Etunimi on pakollinen")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Etunimen tulee olla 3-100 merkkia")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Sukunimi on pakollinen")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Sukunimen tulee olla 3-100 merkkia")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Sahkoposti on pakollinen")]
    [EmailAddress(ErrorMessage = "Sahkopostin tulee olla kelvollinen")]
    [StringLength(255, ErrorMessage = "Sahkoposti voi olla enintaan 255 merkkia")]
    public string Email { get; set; } = string.Empty;
}
