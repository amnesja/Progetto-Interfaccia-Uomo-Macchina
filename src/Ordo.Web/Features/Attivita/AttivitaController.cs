using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ordo.Services;
using Ordo.Services.Shared;
using Ordo.Web.Areas;

namespace Ordo.Web.Features.Attivita
{
    public partial class AttivitaController : AuthenticatedBaseController
    {
        private readonly OrdoDbContext _dbContext;

        public AttivitaController(OrdoDbContext dbContext, SharedService sharedService) : base(sharedService)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        public virtual async Task<IActionResult> Index(TaskState? filtro)
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdValue, out var userId))
                return Challenge();

            var tasks = _dbContext.Tasks
                .AsNoTracking()
                .Where(x => x.AssignedUserId == userId);

            if (filtro.HasValue)
            {
                tasks = tasks.Where(x => x.Stato == filtro.Value);
            }

            var model = new AttivitaViewModel
            {
                Filtro = filtro,
                Attivita = await tasks
                    .OrderBy(x => x.Scadenza == null)
                    .ThenBy(x => x.Scadenza)
                    .ThenBy(x => x.Titolo)
                    .Select(x => new AttivitaTaskViewModel
                    {
                        Id = x.Id,
                        ProjectId = x.Board.ProjectId,
                        Titolo = x.Titolo,
                        Progetto = x.Board.Project.Nome,
                        Board = x.Board.Nome,
                        BoardId = x.BoardId,
                        Stato = x.Stato,
                        Priorita = x.Priorita,
                        Scadenza = x.Scadenza
                    })
                    .ToArrayAsync()
            };

            return View(model);
        }

        [HttpGet]
        public virtual async Task<IActionResult> Data(TaskState? filtro)
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdValue, out var userId))
                return Unauthorized();

            var tasks = _dbContext.Tasks
                .AsNoTracking()
                .Where(x => x.AssignedUserId == userId);

            var states = await tasks.Select(x => x.Stato).ToListAsync();
            var filteredTasks = filtro.HasValue
                ? tasks.Where(x => x.Stato == filtro.Value)
                : tasks;

            var activities = await filteredTasks
                .OrderBy(x => x.Scadenza == null)
                .ThenBy(x => x.Scadenza)
                .ThenBy(x => x.Titolo)
                .Select(x => new
                {
                    x.Id,
                    ProjectId = x.Board.ProjectId,
                    x.Titolo,
                    Progetto = x.Board.Project.Nome,
                    Board = x.Board.Nome,
                    x.Stato,
                    x.Priorita,
                    x.Scadenza
                })
                .ToArrayAsync();

            return Json(new
            {
                activities,
                counts = states
                    .GroupBy(state => state)
                    .ToDictionary(group => group.Key.ToString(), group => group.Count())
            });
        }
    }
}