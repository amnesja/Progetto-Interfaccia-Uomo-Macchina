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
        
        public DashboardController(OrdoDbContext dbContext, SharedService sharedService) : base(sharedService)
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
            var startOfWeek = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
            var endOfWeek = startOfWeek.AddDays(7);
            var dueDatesThisWeek = await tasks
                .Where(x => x.Scadenza.HasValue &&
                            x.Scadenza.Value.Date >= startOfWeek &&
                            x.Scadenza.Value.Date < endOfWeek)
                .Select(x => x.Scadenza.Value.Date)
                .ToArrayAsync();

            var model = new DashboardViewModel
            {
                NomeUtente = !string.IsNullOrWhiteSpace(user.FirstName)
                    ? user.FirstName
                    : (!string.IsNullOrWhiteSpace(user.NickName) ? user.NickName : user.Email),
                AttivitaSettimana = Enumerable.Range(0, 7)
                    .Select(dayOffset => dueDatesThisWeek.Count(date => date == startOfWeek.AddDays(dayOffset)))
                    .ToArray(),

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
                    .Select(x => new DashboardTaskViewModel
                    {
                        Id = x.Id,
                        ProjectId = x.Board.ProjectId,
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
                        Id = x.Id,
                        Nome = x.Nome,
                        Descrizione = x.Descrizione,
                        NumeroBoard = x.Boards.Count,
                        NumeroTask = x.Boards.SelectMany(b => b.Tasks).Count(),
                        NumeroTaskCompletate = x.Boards.SelectMany(b => b.Tasks)
                            .Count(task => task.Stato == TaskState.Done)
                    })
                    .ToArrayAsync()
            };

            return View(model);
        }
    }
}
