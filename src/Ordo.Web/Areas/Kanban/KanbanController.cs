using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Ordo.Services.Shared;

namespace Ordo.Web.Areas.Kanban
{
    [Area("Kanban")]
    public partial class KanbanController : AuthenticatedBaseController
    {
        private readonly SharedService _sharedService;

        public KanbanController(SharedService sharedService)
        {
            _sharedService = sharedService;
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

            return Ok();
        }
    }
}
