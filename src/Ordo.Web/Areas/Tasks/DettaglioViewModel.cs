using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Ordo.Services.Shared;

namespace Ordo.Web.Areas.Tasks
{
    public class DettaglioViewModel
    {
        public Guid Id { get; set; }
        public string Titolo { get; set; }
        public string Descrizione { get; set; }
        public Priorita Priorita { get; set; }
        public TaskState Stato { get; set; }
        public DateTime? Scadenza { get; set; }
        public Guid BoardId { get; set; }
        public string BoardNome { get; set; }
        public Guid ProjectId { get; set; }
        public string AssignedUserNome { get; set; }
        public bool CanEdit { get; set; }

        public IEnumerable<CommentItemViewModel> Commenti { get; set; } = Array.Empty<CommentItemViewModel>();

        public string StatoLabel => Stato switch
        {
            TaskState.ToDo => "Da fare",
            TaskState.InProgress => "In corso",
            TaskState.Review => "Review",
            TaskState.Done => "Done",
            _ => Stato.ToString()
        };

        public string PrioritaLabel => Priorita switch
        {
            Priorita.Bassa => "Bassa",
            Priorita.Media => "Media",
            Priorita.Alta => "Alta",
            _ => Priorita.ToString()
        };
    }

    public class CommentItemViewModel
    {
        public Guid Id { get; set; }
        public string Testo { get; set; }
        public DateTime DataCreazione { get; set; }
        public Guid UserId { get; set; }
        public string UserNickName { get; set; }
    }

    public class CommentFormViewModel
    {
        [Required(ErrorMessage = "Scrivi un commento prima di inviarlo.")]
        public string Testo { get; set; }

        public Guid TaskId { get; set; }
    }
}