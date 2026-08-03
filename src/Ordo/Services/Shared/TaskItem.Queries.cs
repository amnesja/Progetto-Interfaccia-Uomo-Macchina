using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ordo.Services.Shared
{
    // Query pensata per la Kanban Board: restituisce tutti i task di una Board
    // gia' pronti per essere raggruppati per colonna (Stato) lato Vue.js
    public class TasksByBoardQuery
    {
        public Guid BoardId { get; set; }
    }

    public class TasksByBoardDTO
    {
        public IEnumerable<Task> Tasks { get; set; }

        public class Task
        {
            public Guid Id { get; set; }
            public string Titolo { get; set; }
            public Priorita Priorita { get; set; }
            public TaskState Stato { get; set; }
            public DateTime? Scadenza { get; set; }
            public Guid? AssignedUserId { get; set; }
            public string AssignedUserNickName { get; set; }
        }
    }

    public class TaskDetailQuery
    {
        public Guid Id { get; set; }
    }

    public class TaskDetailDTO
    {
        public Guid Id { get; set; }
        public string Titolo { get; set; }
        public string Descrizione { get; set; }
        public Priorita Priorita { get; set; }
        public TaskState Stato { get; set; }
        public DateTime? Scadenza { get; set; }
        public Guid BoardId { get; set; }
        public Guid? AssignedUserId { get; set; }
    }

    public partial class SharedService
    {
        public async Task<TasksByBoardDTO> Query(TasksByBoardQuery qry)
        {
            var queryable = _dbContext.Tasks
                .Where(x => x.BoardId == qry.BoardId);

            return new TasksByBoardDTO
            {
                Tasks = await queryable
                    .Select(x => new TasksByBoardDTO.Task
                    {
                        Id = x.Id,
                        Titolo = x.Titolo,
                        Priorita = x.Priorita,
                        Stato = x.Stato,
                        Scadenza = x.Scadenza,
                        AssignedUserId = x.AssignedUserId,
                        AssignedUserNickName = x.AssignedUser != null ? x.AssignedUser.NickName : null
                    })
                    .ToArrayAsync()
            };
        }

        public async Task<TaskDetailDTO> Query(TaskDetailQuery qry)
        {
            return await _dbContext.Tasks
                .Where(x => x.Id == qry.Id)
                .Select(x => new TaskDetailDTO
                {
                    Id = x.Id,
                    Titolo = x.Titolo,
                    Descrizione = x.Descrizione,
                    Priorita = x.Priorita,
                    Stato = x.Stato,
                    Scadenza = x.Scadenza,
                    BoardId = x.BoardId,
                    AssignedUserId = x.AssignedUserId
                })
                .FirstOrDefaultAsync();
        }
    }
}
