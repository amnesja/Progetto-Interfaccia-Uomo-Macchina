using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Ordo.Services.Shared;

namespace Ordo.Web.Areas.Tasks
{
    public class EditViewModel
    {
        public Guid? Id { get; set; }
        public Guid BoardId { get; set; }
        public Guid ProjectId { get; set; }

        [Required(ErrorMessage = "Il titolo è obbligatorio.")]
        [Display(Name = "Titolo")]
        public string Titolo { get; set; }

        [Display(Name = "Descrizione")]
        public string Descrizione { get; set; }

        [Display(Name = "Priorità")]
        public Priorita Priorita { get; set; } = Priorita.Media;

        [Display(Name = "Scadenza")]
        [DataType(DataType.Date)]
        public DateTime? Scadenza { get; set; }

        [Display(Name = "Assegnato a")]
        public Guid? AssignedUserId { get; set; }

        public IEnumerable<SelectListItem> UtentiAssegnabili { get; set; } = Array.Empty<SelectListItem>();

        public IEnumerable<SelectListItem> PrioritaOptions { get; } = new[]
        {
            new SelectListItem("Bassa", ((int)Priorita.Bassa).ToString()),
            new SelectListItem("Media", ((int)Priorita.Media).ToString()),
            new SelectListItem("Alta", ((int)Priorita.Alta).ToString())
        };

        public void SetTask(TaskDetailDTO dto)
        {
            Id = dto.Id;
            BoardId = dto.BoardId;
            Titolo = dto.Titolo;
            Descrizione = dto.Descrizione;
            Priorita = dto.Priorita;
            Scadenza = dto.Scadenza;
            AssignedUserId = dto.AssignedUserId;
        }

        public AddOrUpdateTaskCommand ToAddOrUpdateTaskCommand()
        {
            return new AddOrUpdateTaskCommand
            {
                Id = Id,
                Titolo = Titolo,
                Descrizione = Descrizione,
                Priorita = Priorita,
                Scadenza = Scadenza,
                BoardId = BoardId,
                AssignedUserId = AssignedUserId
            };
        }
    }
}