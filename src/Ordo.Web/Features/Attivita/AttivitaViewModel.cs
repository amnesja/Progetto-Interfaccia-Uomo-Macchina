using System;
using System.Collections.Generic;
using Ordo.Services.Shared;

namespace Ordo.Web.Features.Attivita
{
    public class AttivitaViewModel
    {
        public TaskState? Filtro { get; set; }

        public IEnumerable<AttivitaTaskViewModel> Attivita { get; set; } = Array.Empty<AttivitaTaskViewModel>();
    }

    public class AttivitaTaskViewModel
    {
        public Guid Id { get; set; }
        public string Titolo { get; set; }
        public string Progetto { get; set; }
        public string Board { get; set; }
        public Guid BoardId { get; set; }
        public TaskState Stato { get; set; }
        public Priorita Priorita { get; set; }
        public DateTime? Scadenza { get; set; }
    }
}