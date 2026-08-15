using System.ComponentModel.DataAnnotations;

namespace Ordo.Web.Features.Profile
{
    public class ProfileViewModel
    {
        public string Email { get; set; }
        public string NomeCompleto { get; set; }
        public string NickName { get; set; }
    }

    public class ProfileEditViewModel
    {
        public string Email { get; set; }

        [Required(ErrorMessage = "Il nome è obbligatorio.")]
        [Display(Name = "Nome")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Il cognome è obbligatorio.")]
        [Display(Name = "Cognome")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Il nome utente è obbligatorio.")]
        [Display(Name = "Nome utente")]
        public string NickName { get; set; }
    }
}
