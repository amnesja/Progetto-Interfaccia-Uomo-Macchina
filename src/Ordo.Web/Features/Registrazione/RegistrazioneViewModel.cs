using System.ComponentModel.DataAnnotations;

namespace Ordo.Web.Features.Registrazione;

public class RegistrazioneViewModel
{
    [Required]
    [Display(Name = "Email")]
    [DataType(DataType.EmailAddress)]
    public string Email { get; set; }
    
    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    [MinLength(6, ErrorMessage = "La password deve essere almeno 6 caratteri")]
    public string Password { get; set; }
    
    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Conferma password")]
    [Compare(nameof(Password), ErrorMessage = "Le password non coincidono")]
    public string ConfermaPassword { get; set; }
    
    [Required]
    [Display(Name = "Nome")]
    public string FirstName { get; set; }
    
    [Required]
    [Display(Name = "LastName")]
    public string LastName { get; set; }
    
    [Required]
    [Display(Name = "NickName")]
    public string NickName { get; set; }
}