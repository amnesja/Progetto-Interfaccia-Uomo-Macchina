using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Ordo.Services.Shared
{
    public class AddOrUpdateTaskCommand
    {
        public Guid? Id { get; set; }
        public string Titolo { get; set; }
        public string Descrizione { get; set; }
        public Priorita Priorita { get; set; }
        public DateTime? Scadenza { get; set; }
        public Guid BoardId { get; set; }
        public Guid? AssignedUserId { get; set; }
    }

    // Comando dedicato: usalo quando l'utente sposta la card via drag&drop sulla Kanban Board
    public class MoveTaskCommand
    {
        public Guid Id { get; set; }
        public TaskState NuovoStato { get; set; }
    }

    public class DeleteTaskCommand
    {
        public Guid Id { get; set; }
    }

    public partial class SharedService
    {
        public async Task<Guid> Handle(AddOrUpdateTaskCommand cmd)
        {
            var task = await _dbContext.Tasks
                .Where(x => x.Id == cmd.Id)
                .FirstOrDefaultAsync();

            if (task == null)
            {
                task = new TaskItem
                {
                    BoardId = cmd.BoardId,
                    Stato = TaskState.ToDo,
                };
                _dbContext.Tasks.Add(task);
            }

            task.Titolo = cmd.Titolo;
            task.Descrizione = cmd.Descrizione;
            task.Priorita = cmd.Priorita;
            task.Scadenza = cmd.Scadenza;
            task.AssignedUserId = cmd.AssignedUserId;

            await _dbContext.SaveChangesAsync();

            return task.Id;
        }

        public async Task Handle(MoveTaskCommand cmd)
        {
            var task = await _dbContext.Tasks
                .Where(x => x.Id == cmd.Id)
                .FirstOrDefaultAsync();

            if (task == null) return;

            task.Stato = cmd.NuovoStato;

            await _dbContext.SaveChangesAsync();

            // Qui, dopo il salvataggio, pubblicherete l'evento SignalR TaskMovedEvent
            // (lo fate nel Controller, dopo aver chiamato questo Handle)
        }

        public async Task Handle(DeleteTaskCommand cmd)
        {
            var task = await _dbContext.Tasks
                .Where(x => x.Id == cmd.Id)
                .FirstOrDefaultAsync();

            if (task == null) return;

            _dbContext.Tasks.Remove(task);

            await _dbContext.SaveChangesAsync();
        }
    }
}
