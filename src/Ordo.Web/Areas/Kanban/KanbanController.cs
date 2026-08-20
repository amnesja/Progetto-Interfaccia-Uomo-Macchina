using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using Ordo.Services.Shared;
using Ordo.Web.SignalR;
using Ordo.Web.SignalR.Hubs.Events;

namespace Ordo.Web.Areas.Kanban
{
    [Area("Kanban")]
    public partial class KanbanController : AuthenticatedBaseController
    {
        private readonly SharedService _sharedService;
        private readonly IPublishDomainEvents _publisher;

        public KanbanController(SharedService sharedService, IPublishDomainEvents publisher)
        {
            _sharedService = sharedService;
            _publisher = publisher;
        }

        [HttpGet]
        public virtual async Task<IActionResult> Board(Guid id)
        {
            var boardDetail = await _sharedService.Query(new BoardDetailQuery { Id = id });
            if (boardDetail == null) return NotFound();

            var tasks = await _sharedService.Query(new TasksByBoardQuery { BoardId = id });

            var model = new BoardViewModel
            {
                BoardId = boardDetail.Id,
                BoardName = boardDetail.Nome,
                ProjectId = boardDetail.ProjectId,
                Tasks = tasks.Tasks.Select(t => new TaskCardViewModel
                {
                    Id = t.Id,
                    Titolo = t.Titolo,
                    Priorita = (int)t.Priorita,
                    Stato = (int)t.Stato,
                    Scadenza = t.Scadenza,
                    AssignedUserId = t.AssignedUserId,
                    AssignedUserName = t.AssignedUserNickName
                }).ToList()
            };

            return View(model);
        }

        [HttpPost]
        public virtual async Task<IActionResult> MoveTask([FromBody] MoveTaskRequest request)
        {
            await _sharedService.Handle(new MoveTaskCommand
            {
                Id = request.TaskId,
                NuovoStato = (TaskState)request.NuovoStato
            });

            var task = await _sharedService.Query(new TaskDetailQuery { Id = request.TaskId });

            // Notifica in tempo reale chiunque altro stia guardando questa stessa board
            await _publisher.Publish(new TaskMovedEvent
            {
                IdGroup = task.BoardId,
                TaskId = request.TaskId,
                NuovoStato = (TaskState)request.NuovoStato
            });

            return Ok();
        }
    }
}