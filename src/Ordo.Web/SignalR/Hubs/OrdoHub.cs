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
        
        Task ProjectMemberAdded(Guid projectId, string nome, string descrizione);
        Task ProjectDeleted(Guid projectId);
        Task BoardCreated(Guid projectId, Guid boardId, string nome);
        Task BoardDeleted(Guid projectId, Guid boardId, string nome);
        
        Task ProjectUpdated(Guid projectId, string nome, string descrizione);
        Task BoardUpdated(Guid projectId, Guid boardId, string nome);
        
        Task TaskChangedForUser(string tipo, string titolo, string projectNome, Guid projectId, Guid boardId);
        Task MemberRemoved(Guid projectId, Guid userId);
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