using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Ordo.Services.Shared;
using Ordo.Web.Infrastructure;
using System.Linq;
using Ordo.Web.SignalR;
using Ordo.Web.SignalR.Hubs.Events;
using Microsoft.EntityFrameworkCore;
using Ordo.Services;

namespace Ordo.Web.Areas.Progetti
{
    [Area("Progetti")]
    public partial class ProgettiController : AuthenticatedBaseController
    {
        private readonly SharedService _sharedService;
        private readonly IPublishDomainEvents _publisher;
        private readonly OrdoDbContext _dbContext;

        public ProgettiController(SharedService sharedService, IPublishDomainEvents publisher, OrdoDbContext dbContext)
        {
            _sharedService = sharedService;
            _publisher = publisher;
            _dbContext = dbContext;

            ModelUnbinderHelpers.ModelUnbinders.Add(typeof(IndexViewModel), new SimplePropertyModelUnbinder());
        }

        private bool TryGetCurrentUserId(out Guid userId)
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(value, out userId);
        }

        [HttpGet]
        public virtual async Task<IActionResult> Index(IndexViewModel model)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
                return Challenge();

            var progetti = await _sharedService.Query(model.ToProjectsIndexQuery(currentUserId));
            model.SetProjects(progetti);

            return View(model);
        }

        [HttpGet]
        public virtual IActionResult New()
        {
            return RedirectToAction(Actions.Edit());
        }

        [HttpGet]
        public virtual async Task<IActionResult> Edit(Guid? id)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
                return Challenge();

            var model = new EditViewModel();

            if (id.HasValue)
            {
                var progetto = await _sharedService.Query(new ProjectDetailQuery { Id = id.Value });

                if (progetto == null || progetto.OwnerId != currentUserId)
                    return Forbid();

                model.SetProject(progetto);
            }

            return View(model);
        }

        [HttpPost]
        public virtual async Task<IActionResult> Edit(EditViewModel model)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
                return Challenge();

            var isEditingExisting = model.Id.HasValue;

            if (isEditingExisting)
            {
                var esistente = await _sharedService.Query(new ProjectDetailQuery { Id = model.Id.Value });
                if (esistente == null || esistente.OwnerId != currentUserId)
                    return Forbid();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    model.Id = await _sharedService.Handle(model.ToAddOrUpdateProjectCommand(currentUserId));

                    if (isEditingExisting)
                    {
                        var membri = await _sharedService.Query(new ProjectMembersQuery { ProjectId = model.Id.Value });
                        var utentiCoinvolti = membri.Members.Select(m => m.UserId)
                            .Append(currentUserId)
                            .ToArray();

                        await _publisher.Publish(new ProjectUpdatedEvent
                        {
                            ProjectId = model.Id.Value,
                            Nome = model.Nome,
                            Descrizione = model.Descrizione,
                            UtentiCoinvolti = utentiCoinvolti
                        });
                    }

                    Alerts.AddSuccess(this, "Progetto salvato correttamente");
                }
                catch (Exception e)
                {
                    ModelState.AddModelError(string.Empty, e.Message);
                }
            }

            if (ModelState.IsValid == false)
            {
                Alerts.AddError(this, "Errore in salvataggio del progetto");
                return RedirectToAction(Actions.Edit(model.Id));
            }

            return RedirectToAction(Actions.Dettaglio(model.Id.Value));
        }

        [HttpPost]
        public virtual async Task<IActionResult> Delete(Guid id)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
                return Challenge();

            var progetto = await _sharedService.Query(new ProjectDetailQuery { Id = id });

            if (progetto == null || progetto.OwnerId != currentUserId)
                return Forbid();

            // Raccogliamo PRIMA della cancellazione chi va notificato:
            // dopo la Delete, i ProjectMembers vengono eliminati in cascata
            var membri = await _sharedService.Query(new ProjectMembersQuery { ProjectId = id });
            var utentiCoinvolti = membri.Members.Select(m => m.UserId)
                .Append(progetto.OwnerId)
                .ToArray();

            await _sharedService.Handle(new DeleteProjectCommand { Id = id });

            await _publisher.Publish(new ProjectDeletedEvent
            {
                ProjectId = id,
                UtentiCoinvolti = utentiCoinvolti
            });

            Alerts.AddSuccess(this, "Progetto eliminato");

            return RedirectToAction(Actions.Index());
        }

        [HttpGet]
        public virtual async Task<IActionResult> Dettaglio(Guid id)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
                return Challenge();

            var progetto = await _sharedService.Query(new ProjectDetailQuery { Id = id });
            if (progetto == null)
                return NotFound();

            var membri = await _sharedService.Query(new ProjectMembersQuery { ProjectId = id });
            var isOwner = progetto.OwnerId == currentUserId;
            var isMember = membri.Members.Any(m => m.UserId == currentUserId);

            if (!isOwner && !isMember)
                return Forbid();

            var boards = await _sharedService.Query(new BoardsByProjectQuery { ProjectId = id });

            var model = new DettaglioViewModel();
            model.SetProject(progetto, isOwner);
            model.SetBoards(boards);
            model.SetMembers(membri);

            return View(model);
        }

        [HttpPost]
        public virtual async Task<IActionResult> BoardSalva(BoardFormViewModel model)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
                return Challenge();

            var progetto = await _sharedService.Query(new ProjectDetailQuery { Id = model.ProjectId });

            if (progetto == null || progetto.OwnerId != currentUserId)
                return Forbid();

            if (string.IsNullOrWhiteSpace(model.Nome))
            {
                Alerts.AddError(this, "Il nome della board è obbligatorio");
            }
            else
            {
                var isNewBoard = !model.Id.HasValue;
                var boardId = await _sharedService.Handle(model.ToAddOrUpdateBoardCommand());

                if (isNewBoard)
                {
                    await _publisher.Publish(new BoardCreatedEvent
                    {
                        ProjectId = model.ProjectId,
                        BoardId = boardId,
                        BoardNome = model.Nome
                    });
                }
                else
                {
                    await _publisher.Publish(new BoardUpdatedEvent
                    {
                        ProjectId = model.ProjectId,
                        BoardId = boardId,
                        BoardNome = model.Nome
                    });
                }

                Alerts.AddSuccess(this, "Board salvata correttamente");
            }

            return RedirectToAction(Actions.Dettaglio(model.ProjectId));
        }

        [HttpPost]
        public virtual async Task<IActionResult> BoardElimina(Guid id, Guid projectId)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
                return Challenge();

            var progetto = await _sharedService.Query(new ProjectDetailQuery { Id = projectId });

            if (progetto == null || progetto.OwnerId != currentUserId)
                return Forbid();

            var board = await _sharedService.Query(new BoardDetailQuery { Id = id });

            await _sharedService.Handle(new DeleteBoardCommand { Id = id });

            if (board != null)
            {
                await _publisher.Publish(new BoardDeletedEvent
                {
                    ProjectId = projectId,
                    BoardId = id,
                    BoardNome = board.Nome
                });
            }

            Alerts.AddSuccess(this, "Board eliminata");

            return RedirectToAction(Actions.Dettaglio(projectId));
        }

        [HttpPost]
        public virtual async Task<IActionResult> MembroAggiungi(MemberFormViewModel model)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
                return Challenge();

            var progetto = await _sharedService.Query(new ProjectDetailQuery { Id = model.ProjectId });
            if (progetto == null || progetto.OwnerId != currentUserId)
                return Forbid();

            if (!ModelState.IsValid)
            {
                Alerts.AddError(this, "Inserisci un'email valida");
                return RedirectToAction(Actions.Dettaglio(model.ProjectId));
            }

            var utenti = await _sharedService.Query(new UsersSelectQuery { IdCurrentUser = currentUserId, Filter = model.Email });
            var utente = utenti.Users.FirstOrDefault(u => string.Equals(u.Email, model.Email, StringComparison.OrdinalIgnoreCase));

            if (utente == null)
            {
                Alerts.AddError(this, "Nessun utente registrato trovato con questa email");
            }
            else if (utente.Id == progetto.OwnerId)
            {
                Alerts.AddError(this, "Questo utente è già il proprietario del progetto");
            }
            else
            {
                await _sharedService.Handle(new AddProjectMemberCommand { ProjectId = model.ProjectId, UserId = utente.Id });

                await _publisher.Publish(new MemberAddedEvent
                {
                    IdGroup = utente.Id,
                    ProjectId = progetto.Id,
                    ProjectNome = progetto.Nome,
                    ProjectDescrizione = progetto.Descrizione
                });

                Alerts.AddSuccess(this, "Collaboratore aggiunto al progetto");
            }

            return RedirectToAction(Actions.Dettaglio(model.ProjectId));
        }

        [HttpPost]
        public virtual async Task<IActionResult> MembroRimuovi(Guid projectId, Guid userId)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
                return Challenge();

            var progetto = await _sharedService.Query(new ProjectDetailQuery { Id = projectId });
            if (progetto == null || progetto.OwnerId != currentUserId)
                return Forbid();

            // Raccogliamo PRIMA della rimozione i task assegnati a questo utente in questo progetto,
            // perché dopo Handle() risulteranno già scollegati e non li potremmo più recuperare
            var taskDaNotificare = await _dbContext.Tasks
                .Where(t => t.Board.ProjectId == projectId && t.AssignedUserId == userId)
                .Select(t => new { t.Id, t.Titolo, t.BoardId })
                .ToListAsync();

            await _sharedService.Handle(new RemoveProjectMemberCommand { ProjectId = projectId, UserId = userId });

            await _publisher.Publish(new MemberRemovedEvent
            {
                ProjectId = projectId,
                UserId = userId
            });

            foreach (var task in taskDaNotificare)
            {
                await _publisher.Publish(new TaskChangedForUserEvent
                {
                    IdGroup = userId,
                    Tipo = "Unassigned",
                    Titolo = task.Titolo,
                    ProjectNome = progetto.Nome,
                    ProjectId = projectId,
                    BoardId = task.BoardId
                });
            }

            Alerts.AddSuccess(this, "Collaboratore rimosso dal progetto");

            return RedirectToAction(Actions.Dettaglio(projectId));
        }
    }
}