using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Linq;
using System.Security.Claims;
using Ordo.Web.Infrastructure;
using Ordo.Services.Shared;

namespace Ordo.Web.Areas
{
    [Authorize]
    [Alerts]
    [ModelStateToTempData]
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public partial class AuthenticatedBaseController : Controller
    {
        private readonly SharedService _sharedService;

        public AuthenticatedBaseController()
        {
        }
        public AuthenticatedBaseController(SharedService sharedService)
        {
            _sharedService = sharedService;
        }

        protected IdentitaViewModel Identita
        {
            get
            {
                return (IdentitaViewModel)ViewData[IdentitaViewModel.VIEWDATA_IDENTITACORRENTE_KEY];
            }
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            try
            {
                var isAuthenticated = context.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
                var userExists = false;

                if (isAuthenticated)
                {
                    // NUOVO: verifica che l'utente del cookie esista DAVVERO nel database.
                    // Senza questo controllo, un database resettato (in-memory al riavvio,
                    // o SQLite cancellato a mano) lascia il browser "autenticato" verso un
                    // utente fantasma, causando errori o redirect loop invece di un logout pulito.
                    var idValue = context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (Guid.TryParse(idValue, out var userId))
                    {
                        var user = _sharedService.Query(new UserDetailQuery { Id = userId }).GetAwaiter().GetResult();
                        userExists = user != null;
                    }
                }

                if (isAuthenticated && userExists)
                {
                    ViewData[IdentitaViewModel.VIEWDATA_IDENTITACORRENTE_KEY] = new IdentitaViewModel
                    {
                        EmailUtenteCorrente = context.HttpContext.User.Claims.Where(x => x.Type == ClaimTypes.Email).First().Value
                    };
                }
                else
                {
                    HttpContext.SignOutAsync();
                    this.SignOut();

                    context.Result = new RedirectResult(context.HttpContext.Request.GetEncodedUrl());

                    Alerts.AddError(this, isAuthenticated
                        ? "La tua sessione non è più valida. Effettua di nuovo il login."
                        : "L'utente non possiede i diritti per visualizzare la risorsa richiesta");
                }

                base.OnActionExecuting(context);
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}