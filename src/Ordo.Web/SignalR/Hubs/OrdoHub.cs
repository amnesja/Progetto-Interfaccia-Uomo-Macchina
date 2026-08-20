using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace Ordo.Web.SignalR.Hubs
{
    public interface IOrdoClientEvent
    {
        Task TaskMoved(Guid taskId, int nuovoStato);
        Task TaskCreated(Guid taskId);
        Task CommentAdded(Guid taskId, Guid commentId);
        Task UserAssigned(Guid taskId, Guid userId);
    }

    [Microsoft.AspNetCore.Authorization.Authorize] // Hub utilizzabile solo da utenti autenticati
    public class OrdoHub : Hub<IOrdoClientEvent>
    {
        private readonly IPublishDomainEvents _publisher;

        public OrdoHub(IPublishDomainEvents publisher)
        {
            _publisher = publisher;
        }

        // Chiamato dal client quando entra nella pagina di una Board (vedi Board.cshtml)
        public async Task JoinGroup(Guid idGroup)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, idGroup.ToString());
        }

        public async Task LeaveGroup(Guid idGroup)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, idGroup.ToString());
        }
    }
}