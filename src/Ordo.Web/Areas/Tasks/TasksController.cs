using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Ordo.Services.Shared;
using Ordo.Web.Infrastructure;
using Ordo.Web.SignalR;
using Ordo.Web.SignalR.Hubs.Events;

namespace Ordo.Web.Areas.Tasks
{
    [Area("Tasks")]
    public partial class TasksController : AuthenticatedBaseController
    {
        private readonly SharedService _sharedService;
        private readonly IPublishDomainEvents _publisher;

        public TasksController(SharedService sharedService, IPublishDomainEvents publisher)
        {
            _sharedService = sharedService;
            _publisher = publisher;
        }

        private bool TryGetCurrentUserId(out Guid userId)
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(value, out userId);
        }

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

            TaskDetailDTO existingTask = null;
            if (model.Id.HasValue)
            {
                existingTask = await _sharedService.Query(new TaskDetailQuery { Id = model.Id.Value });
                if (existingTask == null) return NotFound();

                if (existingTask.BoardId != model.BoardId)
                    return BadRequest();
            }

            var board = await _sharedService.Query(new BoardDetailQuery { Id = model.BoardId });
            if (board == null) return NotFound();

            var (hasAccess, _, progetto) = await CheckAccess(board.ProjectId, currentUserId);
            if (!hasAccess) return Forbid();

            if (ModelState.IsValid)
            {
                try
                {
                    // Teniamo traccia se è una creazione (id non ancora assegnato) PRIMA di salvare,
                    // perché dopo Handle() model.Id sarà sempre valorizzato in entrambi i casi.
                    var isNewTask = existingTask == null;
                    var oldAssignedUserId = existingTask?.AssignedUserId;
                    var command = model.ToAddOrUpdateTaskCommand();

                    model.Id = await _sharedService.Handle(command);
                    var savedTask = await _sharedService.Query(new TaskDetailQuery { Id = model.Id.Value });

                    string assignedUserName = null;
                    if (command.AssignedUserId.HasValue)
                    {
                        var users = await GetUtentiAssegnabili(board.ProjectId, progetto);
                        assignedUserName = users.FirstOrDefault(user => user.Value == command.AssignedUserId.Value.ToString())?.Text;
                    }

                    var taskEvent = new TaskCreatedEvent
                    {
                        IdGroup = board.Id,
                        TaskId = model.Id.Value,
                        Titolo = savedTask.Titolo,
                        Priorita = (int)savedTask.Priorita,
                        Stato = (int)savedTask.Stato,
                        Scadenza = savedTask.Scadenza,
                        AssignedUserId = savedTask.AssignedUserId,
                        AssignedUserName = assignedUserName
                    };

                    var isAssignmentChanged = !isNewTask && oldAssignedUserId != savedTask.AssignedUserId;

                    // --- Eventi per chi sta guardando la Kanban board in questo momento ---
                    if (isNewTask)
                    {
                        await _publisher.Publish(taskEvent);
                    }
                    else
                    {
                        await _publisher.Publish(new TaskUpdatedEvent
                        {
                            Task = taskEvent,
                            IsAssignmentChanged = isAssignmentChanged
                        });
                    }

                    if (isAssignmentChanged)
                    {
                        await _publisher.Publish(new UserAssignedEvent
                        {
                            IdGroup = board.Id,
                            TaskId = model.Id.Value,
                            UserId = savedTask.AssignedUserId,
                            AssignedUserName = assignedUserName,
                            Titolo = savedTask.Titolo
                        });
                    }

                    // --- Notifiche personali per Dashboard / "Le mie attività", anche a chi
                    //     non sta guardando questa board in questo momento ---
                    var newAssignedUserId = savedTask.AssignedUserId;

                    if (isNewTask)
                    {
                        // task nuovo con assegnatario già impostato in creazione: notifica subito
                        if (newAssignedUserId.HasValue)
                        {
                            await _publisher.Publish(new TaskChangedForUserEvent
                            {
                                IdGroup = newAssignedUserId.Value,
                                Tipo = "Assigned",
                                Titolo = savedTask.Titolo,
                                ProjectNome = progetto.Nome,
                                ProjectId = board.ProjectId,
                                BoardId = board.Id,
                                TaskId = savedTask.Id
                            });
                        }
                    }
                    else if (oldAssignedUserId != newAssignedUserId)
                    {
                        if (oldAssignedUserId.HasValue)
                        {
                            await _publisher.Publish(new TaskChangedForUserEvent
                            {
                                IdGroup = oldAssignedUserId.Value,
                                Tipo = "Unassigned",
                                Titolo = savedTask.Titolo,
                                ProjectNome = progetto.Nome,
                                ProjectId = board.ProjectId,
                                BoardId = board.Id,
                                TaskId = savedTask.Id
                            });
                        }

                        if (newAssignedUserId.HasValue)
                        {
                            await _publisher.Publish(new TaskChangedForUserEvent
                            {
                                IdGroup = newAssignedUserId.Value,
                                Tipo = "Assigned",
                                Titolo = savedTask.Titolo,
                                ProjectNome = progetto.Nome,
                                ProjectId = board.ProjectId,
                                BoardId = board.Id,
                                TaskId = savedTask.Id
                            });
                        }
                    }
                    else if (newAssignedUserId.HasValue)
                    {
                        // stesso assegnatario di prima: se ha cambiato titolo/stato/scadenza, aggiorniamo comunque la sua vista
                        await _publisher.Publish(new TaskChangedForUserEvent
                        {
                            IdGroup = newAssignedUserId.Value,
                            Tipo = "Updated",
                            Titolo = savedTask.Titolo,
                            ProjectNome = progetto.Nome,
                            ProjectId = board.ProjectId,
                            BoardId = board.Id,
                            TaskId = savedTask.Id
                        });
                    }

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

            var task = await _sharedService.Query(new TaskDetailQuery { Id = id });
            if (task == null) return NotFound();

            if (task.BoardId != boardId)
                return BadRequest();

            var board = await _sharedService.Query(new BoardDetailQuery { Id = task.BoardId });
            if (board == null) return NotFound();

            var (hasAccess, _, progetto) = await CheckAccess(board.ProjectId, currentUserId);
            if (!hasAccess) return Forbid();

            await _sharedService.Handle(new DeleteTaskCommand { Id = id });

            await _publisher.Publish(new TaskDeletedEvent
            {
                IdGroup = board.Id,
                TaskId = task.Id,
                Titolo = task.Titolo
            });

            if (task.AssignedUserId.HasValue)
            {
                await _publisher.Publish(new TaskChangedForUserEvent
                {
                    IdGroup = task.AssignedUserId.Value,
                    Tipo = "Deleted",
                    Titolo = task.Titolo,
                    ProjectNome = progetto.Nome,
                    ProjectId = board.ProjectId,
                    BoardId = board.Id,
                    TaskId = task.Id
                });
            }

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
            var (hasAccess, _, progetto) = await CheckAccess(board.ProjectId, currentUserId);
            if (!hasAccess) return Forbid();

            if (string.IsNullOrWhiteSpace(model.Testo))
            {
                Alerts.AddError(this, "Scrivi un commento prima di inviarlo");
            }
            else
            {
                var commentId = await _sharedService.Handle(new AddCommentCommand
                {
                    Testo = model.Testo,
                    TaskId = model.TaskId,
                    UserId = currentUserId
                });

                await _publisher.Publish(new CommentAddedEvent
                {
                    IdGroup = board.Id,
                    TaskId = model.TaskId,
                    CommentId = commentId,
                    Titolo = task.Titolo
                });

                if (task.AssignedUserId.HasValue && task.AssignedUserId != currentUserId)
                {
                    await _publisher.Publish(new TaskChangedForUserEvent
                    {
                        IdGroup = task.AssignedUserId.Value,
                        Tipo = "Commented",
                        Titolo = task.Titolo,
                        ProjectNome = progetto.Nome,
                        ProjectId = board.ProjectId,
                        BoardId = board.Id,
                        TaskId = task.Id
                    });
                }
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

            if (commento == null || commento.UserId != currentUserId)
                return Forbid();

            var task = await _sharedService.Query(new TaskDetailQuery { Id = taskId });
            if (task == null) return NotFound();

            var board = await _sharedService.Query(new BoardDetailQuery { Id = task.BoardId });
            if (board == null) return NotFound();

            var (hasAccess, _, progetto) = await CheckAccess(board.ProjectId, currentUserId);
            if (!hasAccess) return Forbid();

            await _sharedService.Handle(new DeleteCommentCommand { Id = id });
            await _publisher.Publish(new CommentDeletedEvent
            {
                IdGroup = board.Id,
                TaskId = taskId,
                CommentId = id,
                Titolo = task.Titolo
            });

            if (task.AssignedUserId.HasValue && task.AssignedUserId != currentUserId)
            {
                await _publisher.Publish(new TaskChangedForUserEvent
                {
                    IdGroup = task.AssignedUserId.Value,
                    Tipo = "CommentDeleted",
                    Titolo = task.Titolo,
                    ProjectNome = progetto.Nome,
                    ProjectId = board.ProjectId,
                    BoardId = board.Id,
                    TaskId = task.Id
                });
            }

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
