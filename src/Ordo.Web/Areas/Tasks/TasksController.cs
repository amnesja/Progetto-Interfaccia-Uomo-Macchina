using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Ordo.Services.Shared;
using Ordo.Web.Infrastructure;

namespace Ordo.Web.Areas.Tasks
{
    [Area("Tasks")]
    public partial class TasksController : AuthenticatedBaseController
    {
        private readonly SharedService _sharedService;

        public TasksController(SharedService sharedService)
        {
            _sharedService = sharedService;
        }

        private bool TryGetCurrentUserId(out Guid userId)
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(value, out userId);
        }

        // Verifica se l'utente corrente può vedere/modificare i task di questo progetto
        // (stessa regola usata in ProgettiController: proprietario o collaboratore)
        private async Task<(bool hasAccess, bool isOwner, ProjectDetailDTO progetto)> CheckAccess(Guid projectId, Guid currentUserId)
        {
            var progetto = await _sharedService.Query(new ProjectDetailQuery { Id = projectId });
            if (progetto == null) return (false, false, null);

            if (progetto.OwnerId == currentUserId) return (true, true, progetto);

            var membri = await _sharedService.Query(new ProjectMembersQuery { ProjectId = projectId });
            var isMember = membri.Members.Any(m => m.UserId == currentUserId);

            return (isMember, false, progetto);
        }

        [HttpGet]
        public virtual IActionResult New(Guid boardId)
        {
            return RedirectToAction(Actions.Edit(null, boardId));
        }

        [HttpGet]
        public virtual async Task<IActionResult> Edit(Guid? id, Guid? boardId)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
                return Challenge();

            Guid effectiveBoardId;
            TaskDetailDTO task = null;

            if (id.HasValue)
            {
                task = await _sharedService.Query(new TaskDetailQuery { Id = id.Value });
                if (task == null) return NotFound();
                effectiveBoardId = task.BoardId;
            }
            else if (boardId.HasValue)
            {
                effectiveBoardId = boardId.Value;
            }
            else
            {
                return BadRequest();
            }

            var board = await _sharedService.Query(new BoardDetailQuery { Id = effectiveBoardId });
            if (board == null) return NotFound();

            var (hasAccess, _, progetto) = await CheckAccess(board.ProjectId, currentUserId);
            if (!hasAccess) return Forbid();

            var model = new EditViewModel
            {
                BoardId = effectiveBoardId,
                ProjectId = board.ProjectId
            };

            if (task != null)
                model.SetTask(task);

            model.UtentiAssegnabili = await GetUtentiAssegnabili(board.ProjectId, progetto);

            return View(model);
        }

        [HttpPost]
        public virtual async Task<IActionResult> Edit(EditViewModel model)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
                return Challenge();

            var board = await _sharedService.Query(new BoardDetailQuery { Id = model.BoardId });
            if (board == null) return NotFound();

            var (hasAccess, _, progetto) = await CheckAccess(board.ProjectId, currentUserId);
            if (!hasAccess) return Forbid();

            if (ModelState.IsValid)
            {
                try
                {
                    model.Id = await _sharedService.Handle(model.ToAddOrUpdateTaskCommand());

                    Alerts.AddSuccess(this, "Task salvato correttamente");

                    return RedirectToAction(Actions.Dettaglio(model.Id.Value));
                }
                catch (Exception e)
                {
                    ModelState.AddModelError(string.Empty, e.Message);
                }
            }

            Alerts.AddError(this, "Errore in salvataggio del task");
            model.UtentiAssegnabili = await GetUtentiAssegnabili(board.ProjectId, progetto);
            return View(model);
        }

        [HttpPost]
        public virtual async Task<IActionResult> Delete(Guid id, Guid boardId)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
                return Challenge();

            var board = await _sharedService.Query(new BoardDetailQuery { Id = boardId });
            if (board == null) return NotFound();

            var (hasAccess, _, _) = await CheckAccess(board.ProjectId, currentUserId);
            if (!hasAccess) return Forbid();

            await _sharedService.Handle(new DeleteTaskCommand { Id = id });

            Alerts.AddSuccess(this, "Task eliminato");

            return RedirectToAction(MVC.Kanban.Kanban.Board(boardId));
        }

        [HttpGet]
        public virtual async Task<IActionResult> Dettaglio(Guid id)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
                return Challenge();

            var task = await _sharedService.Query(new TaskDetailQuery { Id = id });
            if (task == null) return NotFound();

            var board = await _sharedService.Query(new BoardDetailQuery { Id = task.BoardId });
            if (board == null) return NotFound();

            var (hasAccess, _, progetto) = await CheckAccess(board.ProjectId, currentUserId);
            if (!hasAccess) return Forbid();

            var commenti = await _sharedService.Query(new CommentsByTaskQuery { TaskId = id });

            string assignedNome = null;
            if (task.AssignedUserId.HasValue)
            {
                var utenti = await GetUtentiAssegnabili(board.ProjectId, progetto);
                assignedNome = utenti.FirstOrDefault(u => u.Value == task.AssignedUserId.Value.ToString())?.Text;
            }

            ViewBag.CurrentUserId = currentUserId;

            var model = new DettaglioViewModel
            {
                Id = task.Id,
                Titolo = task.Titolo,
                Descrizione = task.Descrizione,
                Priorita = task.Priorita,
                Stato = task.Stato,
                Scadenza = task.Scadenza,
                BoardId = board.Id,
                BoardNome = board.Nome,
                ProjectId = board.ProjectId,
                AssignedUserNome = assignedNome,
                CanEdit = hasAccess,
                Commenti = commenti.Comments.Select(c => new CommentItemViewModel
                {
                    Id = c.Id,
                    Testo = c.Testo,
                    DataCreazione = c.DataCreazione,
                    UserId = c.UserId,
                    UserNickName = c.UserNickName
                }).ToArray()
            };

            return View(model);
        }

        [HttpPost]
        public virtual async Task<IActionResult> CommentoAggiungi(CommentFormViewModel model)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
                return Challenge();

            var task = await _sharedService.Query(new TaskDetailQuery { Id = model.TaskId });
            if (task == null) return NotFound();

            var board = await _sharedService.Query(new BoardDetailQuery { Id = task.BoardId });
            var (hasAccess, _, _) = await CheckAccess(board.ProjectId, currentUserId);
            if (!hasAccess) return Forbid();

            if (string.IsNullOrWhiteSpace(model.Testo))
            {
                Alerts.AddError(this, "Scrivi un commento prima di inviarlo");
            }
            else
            {
                await _sharedService.Handle(new AddCommentCommand
                {
                    Testo = model.Testo,
                    TaskId = model.TaskId,
                    UserId = currentUserId
                });
            }

            return RedirectToAction(Actions.Dettaglio(model.TaskId));
        }

        [HttpPost]
        public virtual async Task<IActionResult> CommentoElimina(Guid id, Guid taskId)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
                return Challenge();

            var commenti = await _sharedService.Query(new CommentsByTaskQuery { TaskId = taskId });
            var commento = commenti.Comments.FirstOrDefault(c => c.Id == id);

            // solo l'autore del commento può eliminarlo
            if (commento == null || commento.UserId != currentUserId)
                return Forbid();

            await _sharedService.Handle(new DeleteCommentCommand { Id = id });

            return RedirectToAction(Actions.Dettaglio(taskId));
        }

        private async Task<List<SelectListItem>> GetUtentiAssegnabili(Guid projectId, ProjectDetailDTO progetto)
        {
            var membri = await _sharedService.Query(new ProjectMembersQuery { ProjectId = projectId });
            var owner = await _sharedService.Query(new UserDetailQuery { Id = progetto.OwnerId });

            var lista = new List<SelectListItem>();

            if (owner != null)
            {
                lista.Add(new SelectListItem($"{owner.FirstName} {owner.LastName} (proprietario)", owner.Id.ToString()));
            }

            lista.AddRange(membri.Members.Select(m =>
                new SelectListItem(
                    string.IsNullOrWhiteSpace(m.FirstName) ? m.Email : $"{m.FirstName} {m.LastName}",
                    m.UserId.ToString())));

            return lista;
        }
    }
}