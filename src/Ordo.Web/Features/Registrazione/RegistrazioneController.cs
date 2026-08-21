using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Ordo.Infrastructure;
using Ordo.Services.Shared;
using Ordo.Web.Infrastructure;

namespace Ordo.Web.Features.Registrazione
{
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    [Alerts]
    [ModelStateToTempData]
    public partial class RegistrazioneController : Controller
    {
        public static string RegistrazioneErrorModelKey = "RegistrazioneError";
        private readonly SharedService _sharedService;
        private readonly IStringLocalizer<SharedResource> _sharedLocalizer;

        public RegistrazioneController(SharedService sharedService, IStringLocalizer<SharedResource> sharedLocalizer)
        {
            _sharedService = sharedService;
            _sharedLocalizer = sharedLocalizer;
        }

        [HttpGet]
        public virtual IActionResult Registrazione()
        {
            if (HttpContext.User != null && HttpContext.User.Identity != null &&
                HttpContext.User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            var model = new RegistrazioneViewModel();
            return View(model);
        }

        [HttpPost]
        public async virtual Task<ActionResult> Registrazione(RegistrazioneViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    await _sharedService.Handle(new RegisterUserCommand
                    {
                        Email = model.Email,
                        Password = model.Password,
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        NickName = model.NickName,
                    });
                    
                    Alerts.AddSuccess(this, "Registrazione completata");
                    return RedirectToAction("Login", "Login", new { returnUrl = "/Dashboard" });
                }
                catch (EmailAlreadyExistException e)
                {
                    ModelState.AddModelError(RegistrazioneErrorModelKey, e.Message);
                }
            }

            return RedirectToAction(MVC.Registrazione.Registrazione());
        }
    }
}
