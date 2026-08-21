using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ordo.Services;
using Ordo.Services.Shared;
using Ordo.Web.Areas;

namespace Ordo.Web.Features.Dashboard
{
    public partial class DashboardController : AuthenticatedBaseController
    {
        private readonly OrdoDbContext _dbContext;
        
        public DashboardController(OrdoDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        
        [HttpGet]
        public virtual async Task<IActionResult> Index()
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(userIdValue, out var userId))
                return Challenge();

            var user = await _dbContext.Users
                .AsNoTracking()
                .Where(x => x.Id == userId)
                .Select(x => new { x.FirstName, x.NickName, x.Email })
                .SingleOrDefaultAsync();

            if (user == null)
                return Challenge();

            var tasks = _dbContext.Tasks
                .AsNoTracking()
                .Where(x => x.AssignedUserId == userId);

            var today = DateTime.Today;

            var model = new DashboardViewModel
            {
                NomeUtente = !string.IsNullOrWhiteSpace(user.FirstName)
                    ? user.FirstName
                    : (!string.IsNullOrWhiteSpace(user.NickName) ? user.NickName : user.Email),

                AttivitaDaFare = await tasks.CountAsync(x => x.Stato == TaskState.ToDo),
                AttivitaInCorso = await tasks.CountAsync(x => x.Stato == TaskState.InProgress),
                AttivitaInRevisione = await tasks.CountAsync(x => x.Stato == TaskState.Review),
                AttivitaScadute = await tasks.CountAsync(x =>
                    x.Stato != TaskState.Done &&
                    x.Scadenza.HasValue &&
                    x.Scadenza.Value.Date < today),

                Attivita = await tasks
                    .Where(x => x.Stato != TaskState.Done)
                    .OrderBy(x => x.Scadenza == null)
                    .ThenBy(x => x.Scadenza)
                    .ThenBy(x => x.Titolo)
                    .Take(6)
                    .Select(x => new DashboardTaskViewModel
                    {
                        Titolo = x.Titolo,
                        Progetto = x.Board.Project.Nome,
                        Board = x.Board.Nome,
                        Stato = x.Stato,
                        Priorita = x.Priorita,
                        Scadenza = x.Scadenza
                    })
                    .ToArrayAsync(),

                Progetti = await _dbContext.Projects
                    .AsNoTracking()
                    .Where(x => x.OwnerId == userId)
                    .OrderBy(x => x.Nome)
                    .Take(4)
                    .Select(x => new DashboardProjectViewModel
                    {
                        Nome = x.Nome,
                        Descrizione = x.Descrizione,
                        NumeroBoard = x.Boards.Count,
                        NumeroTask = x.Boards.SelectMany(b => b.Tasks).Count()
                    })
                    .ToArrayAsync()
            };

            return View(model);
        }
    }
}
