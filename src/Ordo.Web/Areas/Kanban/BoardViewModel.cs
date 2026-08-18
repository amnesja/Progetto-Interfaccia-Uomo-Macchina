using System;
using System.Collections.Generic;

using Ordo.Web.Infrastructure;

namespace Ordo.Web.Areas.Kanban
{
    public class BoardViewModel
    {
        public Guid BoardId { get; set; }
        public string BoardName { get; set; }
        public Guid ProjectId { get; set; }

        public List<TaskCardViewModel> Tasks { get; set; } = new List<TaskCardViewModel>();

        public string ToJson()
        {
            return JsonSerializer.ToJsonCamelCase(this);
        }
    }

    public class TaskCardViewModel
    {
        public Guid Id { get; set; }
        public string Titolo { get; set; }
        public int Priorita { get; set; }       // 0  bassa, 1 media, 2 alta
        public int Stato { get; set; }          // 0 da fare, 1 in corso, 2 revisione e 3 completato
        public DateTime? Scadenza { get; set; }
        public Guid? AssignedUserId { get; set; }
        public string AssignedUserName { get; set; }
    }
    
    // Payload inviato dal client quando una card viene trascinata in un'altra colonna
    public class MoveTaskRequest
    {
        public Guid TaskId { get; set; }
        public int NuovoStato { get; set; }
    }
}