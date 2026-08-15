using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ordo.Services;
using Ordo.Services.Shared;
using Ordo.Web.Areas;

namespace Ordo.Web.Features.Profile
{
    public partial class ProfileController : AuthenticatedBaseController
    {
        private readonly OrdoDbContext _dbContext;
        private readonly SharedService _sharedService;

        public ProfileController(OrdoDbContext dbContext, SharedService sharedService)
        {
            _dbContext = dbContext;
            _sharedService = sharedService;
        }

        [HttpGet("/Profile")]
        [HttpGet("/Profile/Profile")]
        public virtual async Task<IActionResult> Profile()
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdValue, out var userId))
                return Challenge();

            var user = await _dbContext.Users
                .AsNoTracking()
                .Where(x => x.Id == userId)
                .Select(x => new { x.Email, x.FirstName, x.LastName, x.NickName })
                .SingleOrDefaultAsync();

            if (user == null)
                return Challenge();

            var tasks = _dbContext.Tasks
                .AsNoTracking()
                .Where(x => x.AssignedUserId == userId);

            var today = DateTime.Today;
            var model = new ProfileViewModel
            {
                Email = user.Email,
                NomeCompleto = string.Join(" ", new[] { user.FirstName, user.LastName }.Where(x => !string.IsNullOrWhiteSpace(x))),
                NickName = user.NickName,
                AttivitaDaFare = await tasks.CountAsync(x => x.Stato == TaskState.ToDo),
                AttivitaInCorso = await tasks.CountAsync(x => x.Stato == TaskState.InProgress),
                AttivitaInRevisione = await tasks.CountAsync(x => x.Stato == TaskState.Review),
                AttivitaCompletate = await tasks.CountAsync(x => x.Stato == TaskState.Done),
                AttivitaScadute = await tasks.CountAsync(x => x.Stato != TaskState.Done && x.Scadenza.HasValue && x.Scadenza.Value.Date < today),
                AttivitaAperte = await tasks
                    .Where(x => x.Stato != TaskState.Done)
                    .OrderBy(x => x.Scadenza == null)
                    .ThenBy(x => x.Scadenza)
                    .ThenBy(x => x.Titolo)
                    .Select(x => new AttivitaAssegnataViewModel
                    {
                        Titolo = x.Titolo,
                        Progetto = x.Board.Project.Nome,
                        Board = x.Board.Nome,
                        Priorita = x.Priorita,
                        Stato = x.Stato,
                        Scadenza = x.Scadenza
                    })
                    .ToArrayAsync()
            };

            return View(model);
        }

        [HttpGet]
        public virtual async Task<IActionResult> Edit()
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdValue, out var userId))
                return Challenge();

            var model = await _dbContext.Users
                .AsNoTracking()
                .Where(x => x.Id == userId)
                .Select(x => new ProfileEditViewModel
                {
                    Email = x.Email,
                    FirstName = x.FirstName,
                    LastName = x.LastName,
                    NickName = x.NickName
                })
                .SingleOrDefaultAsync();

            return model == null ? Challenge() : View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public virtual async Task<IActionResult> Edit(ProfileEditViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdValue, out var userId))
                return Challenge();

            await _sharedService.Handle(new AddOrUpdateUserCommand
            {
                Id = userId,
                FirstName = model.FirstName,
                LastName = model.LastName,
                NickName = model.NickName
            });

            return RedirectToAction(nameof(Profile));
        }
    }
}
