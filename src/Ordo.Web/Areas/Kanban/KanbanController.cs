using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Security.Claims;
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

        private bool TryGetCurrentUserId(out Guid userId)
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(value, out userId);
        }

        private async Task<bool> HasProjectAccess(Guid projectId, Guid userId)
        {
            var project = await _sharedService.Query(new ProjectDetailQuery { Id = projectId });
            if (project == null)
                return false;

            if (project.OwnerId == userId)
                return true;

            var members = await _sharedService.Query(new ProjectMembersQuery { ProjectId = projectId });
            return members.Members.Any(member => member.UserId == userId);
        }

        [HttpGet]
        public virtual async Task<IActionResult> Board(Guid id)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
                return Challenge();

            var boardDetail = await _sharedService.Query(new BoardDetailQuery { Id = id });
            if (boardDetail == null) return NotFound();

            if (!await HasProjectAccess(boardDetail.ProjectId, currentUserId))
                return Forbid();

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
        [ValidateAntiForgeryToken]
        public virtual async Task<IActionResult> MoveTask([FromBody] MoveTaskRequest request)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
                return Challenge();

            if (!Enum.IsDefined(typeof(TaskState), request.NuovoStato))
                return BadRequest();

            var task = await _sharedService.Query(new TaskDetailQuery { Id = request.TaskId });
            if (task == null) return NotFound();

            var board = await _sharedService.Query(new BoardDetailQuery { Id = task.BoardId });
            if (board == null) return NotFound();

            if (!await HasProjectAccess(board.ProjectId, currentUserId))
                return Forbid();

            await _sharedService.Handle(new MoveTaskCommand
            {
                Id = request.TaskId,
                NuovoStato = (TaskState)request.NuovoStato
            });

            // Notifica in tempo reale chiunque altro stia guardando questa stessa board
            await _publisher.Publish(new TaskMovedEvent
            {
                IdGroup = board.Id,
                TaskId = request.TaskId,
                NuovoStato = (TaskState)request.NuovoStato,
                Titolo = task.Titolo
            });

            // Notifica personale a chi è assegnato al task, anche se non sta guardando questa board
            if (task.AssignedUserId.HasValue)
            {
                var project = await _sharedService.Query(new ProjectDetailQuery { Id = board.ProjectId });

                await _publisher.Publish(new TaskChangedForUserEvent
                {
                    IdGroup = task.AssignedUserId.Value,
                    Tipo = "Updated",
                    Titolo = task.Titolo,
                    ProjectNome = project?.Nome,
                    ProjectId = board.ProjectId,
                    BoardId = task.BoardId
                });
            }

            return Ok();
        }
    }
}