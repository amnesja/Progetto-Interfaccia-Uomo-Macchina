using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Ordo.Services.Shared;

namespace Ordo.Web.Features.Profile
{
    public class ProfileViewModel
    {
        public string Email { get; set; }
        public string NomeCompleto { get; set; }
        public string NickName { get; set; }

        public int AttivitaDaFare { get; set; }
        public int AttivitaInCorso { get; set; }
        public int AttivitaInRevisione { get; set; }
        public int AttivitaCompletate { get; set; }
        public int AttivitaScadute { get; set; }
        public IEnumerable<AttivitaAssegnataViewModel> AttivitaAperte { get; set; } = Array.Empty<AttivitaAssegnataViewModel>();
    }

    public class AttivitaAssegnataViewModel
    {
        public string Titolo { get; set; }
        public string Progetto { get; set; }
        public string Board { get; set; }
        public Priorita Priorita { get; set; }
        public TaskState Stato { get; set; }
        public DateTime? Scadenza { get; set; }
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
