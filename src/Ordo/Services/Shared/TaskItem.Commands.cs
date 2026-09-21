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


    // Comando usato quando l'utente sposta una card
    // tramite drag&drop sulla Kanban Board.
    public class MoveTaskCommand
    {
        public Guid Id { get; set; }

        public TaskState NuovoStato { get; set; }

        public Priorita? NuovaPriorita { get; set; }
    }


    // Comando usato quando l'utente trascina
    // una card sopra un'altra card dello stesso stato.
    //
    // In questo caso le due priorità vengono scambiate.
    public class SwapTaskPriorityCommand
    {
        public Guid FirstTaskId { get; set; }

        public Guid SecondTaskId { get; set; }
    }


    public class DeleteTaskCommand
    {
        public Guid Id { get; set; }
    }


    public partial class SharedService
    {
        public async Task<Guid> Handle(
            AddOrUpdateTaskCommand cmd)
        {
            var task =
                await _dbContext.Tasks
                    .Where(x => x.Id == cmd.Id)
                    .FirstOrDefaultAsync();


            if (task == null)
            {
                task = new TaskItem
                {
                    BoardId = cmd.BoardId,
                    Stato = TaskState.ToDo
                };

                _dbContext.Tasks.Add(task);
            }


            task.Titolo =
                cmd.Titolo;

            task.Descrizione =
                cmd.Descrizione;

            task.Priorita =
                cmd.Priorita;

            task.Scadenza =
                cmd.Scadenza;

            task.AssignedUserId =
                cmd.AssignedUserId;


            await _dbContext.SaveChangesAsync();


            return task.Id;
        }


        public async Task Handle(
            MoveTaskCommand cmd)
        {
            var task =
                await _dbContext.Tasks
                    .Where(x => x.Id == cmd.Id)
                    .FirstOrDefaultAsync();


            if (task == null)
            {
                return;
            }


            task.Stato =
                cmd.NuovoStato;


            if (cmd.NuovaPriorita.HasValue)
            {
                task.Priorita =
                    cmd.NuovaPriorita.Value;
            }


            await _dbContext.SaveChangesAsync();

            // L'evento SignalR relativo allo spostamento
            // viene pubblicato dal Controller dopo
            // l'esecuzione di questo Handle.
        }


        public async Task Handle(
            SwapTaskPriorityCommand cmd)
        {
            if (
                cmd.FirstTaskId ==
                cmd.SecondTaskId
            )
            {
                return;
            }


            var tasks =
                await _dbContext.Tasks
                    .Where(x =>
                        x.Id == cmd.FirstTaskId ||
                        x.Id == cmd.SecondTaskId)
                    .ToArrayAsync();


            var firstTask =
                tasks.FirstOrDefault(
                    x =>
                        x.Id ==
                        cmd.FirstTaskId
                );


            var secondTask =
                tasks.FirstOrDefault(
                    x =>
                        x.Id ==
                        cmd.SecondTaskId
                );


            if (
                firstTask == null ||
                secondTask == null
            )
            {
                return;
            }


            // Le due card devono appartenere
            // alla stessa board.
            if (
                firstTask.BoardId !=
                secondTask.BoardId
            )
            {
                return;
            }


            // Le due card devono appartenere
            // allo stesso stato/colonna.
            if (
                firstTask.Stato !=
                secondTask.Stato
            )
            {
                return;
            }


            var firstPriority =
                firstTask.Priorita;


            firstTask.Priorita =
                secondTask.Priorita;


            secondTask.Priorita =
                firstPriority;


            await _dbContext.SaveChangesAsync();
        }


        public async Task Handle(
            DeleteTaskCommand cmd)
        {
            var task =
                await _dbContext.Tasks
                    .Where(x => x.Id == cmd.Id)
                    .FirstOrDefaultAsync();


            if (task == null)
            {
                return;
            }


            _dbContext.Tasks.Remove(task);


            await _dbContext.SaveChangesAsync();
        }
    }
}