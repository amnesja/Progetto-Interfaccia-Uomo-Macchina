
using System;
using System.Collections.Generic;
using Ordo.Services.Shared;

namespace Ordo.Web.Features.Dashboard
{
    public class DashboardViewModel
    {
        public string NomeUtente { get; set; }
        public int AttivitaDaFare { get; set; }
        public int AttivitaInCorso { get; set; }
        public int AttivitaInRevisione { get; set; }
        public int AttivitaScadute { get; set; }

        public IEnumerable<DashboardTaskViewModel> Attivita { get; set; }
            = Array.Empty<DashboardTaskViewModel>();
        public IEnumerable<DashboardProjectViewModel> Progetti { get; set; }
            = Array.Empty<DashboardProjectViewModel>();
    }

    public class DashboardTaskViewModel
    {
        public string Titolo { get; set; }
        public string Progetto { get; set; }
        public string Board { get; set; }
        public TaskState Stato { get; set; }
        public Priorita Priorita { get; set; }
        public DateTime? Scadenza { get; set; }
    }
    
    public class DashboardProjectViewModel
    {
        public string Nome { get; set; }
        public string Descrizione { get; set; }
        public int NumeroBoard { get; set; }
        public int NumeroTask { get; set; }
    }
}
