using Microsoft.AspNetCore.SignalR;
using Ordo.Services.Shared;
using Ordo.Web.SignalR.Hubs.Events;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Ordo.Web.SignalR.Hubs
{
    public interface IOrdoClientEvent
    {
        Task TaskMoved(Guid taskId, int nuovoStato, string titolo);
        Task TaskCreated(TaskCreatedEvent task);
        Task TaskUpdated(TaskUpdatedEvent task);
        Task TaskDeleted(TaskDeletedEvent task);
        Task CommentAdded(CommentAddedEvent comment);
        Task CommentDeleted(CommentDeletedEvent comment);
        Task UserAssigned(Guid taskId, Guid? userId, string assignedUserName, string titolo);

        Task ProjectMemberAdded(Guid projectId, string nome, string descrizione);
        Task ProjectDeleted(Guid projectId);
        Task BoardCreated(Guid projectId, Guid boardId, string nome);
        Task BoardDeleted(Guid projectId, Guid boardId, string nome);

        Task ProjectUpdated(Guid projectId, string nome, string descrizione);
        Task BoardUpdated(Guid projectId, Guid boardId, string nome);

        Task TaskChangedForUser(string tipo, string titolo, string projectNome, Guid projectId, Guid boardId, Guid taskId);
        Task MemberRemoved(Guid projectId, Guid userId);
        Task ProjectChatMessageAdded(ProjectChatMessageEvent message);
    }

    [Microsoft.AspNetCore.Authorization.Authorize] // Hub utilizzabile solo da utenti autenticati
    public class OrdoHub : Hub<IOrdoClientEvent>
    {
        private readonly SharedService _sharedService;

        public OrdoHub(SharedService sharedService)
        {
            _sharedService = sharedService;
        }

        // Chiamato dal client quando entra nella pagina di una Board, di un Progetto,
        // o quando si iscrive al proprio "canale personale" (per le notifiche individuali,
        // es. TaskChangedForUser, MemberRemoved) - vedi Board.cshtml, Progetti/Dettaglio.cshtml, ecc.
        public async Task JoinGroup(Guid idGroup)
        {
            if (!await HasAccessToGroup(idGroup))
                throw new HubException("Non sei autorizzato ad accedere a questo gruppo.");

            await Groups.AddToGroupAsync(Context.ConnectionId, idGroup.ToString());
        }

        public async Task LeaveGroup(Guid idGroup)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, idGroup.ToString());
        }

        private async Task<bool> HasAccessToGroup(Guid idGroup)
        {
            var userIdValue = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdValue, out var userId))
                return false;

            // Caso 1: il client si iscrive al proprio canale personale (notifiche individuali)
            if (idGroup == userId)
                return true;

            // Caso 2: idGroup è un ProjectId (es. pagina Dettaglio Progetto)
            var project = await _sharedService.Query(new ProjectDetailQuery { Id = idGroup });
            if (project != null)
                return await HasAccessToProject(project.Id, userId, project.OwnerId);

            // Caso 3: idGroup è un BoardId (es. Kanban) -> risali al progetto della board
            var board = await _sharedService.Query(new BoardDetailQuery { Id = idGroup });
            if (board != null)
            {
                var boardProject = await _sharedService.Query(new ProjectDetailQuery { Id = board.ProjectId });
                if (boardProject != null)
                    return await HasAccessToProject(boardProject.Id, userId, boardProject.OwnerId);
            }

            return false;
        }

        private async Task<bool> HasAccessToProject(Guid projectId, Guid userId, Guid ownerId)
        {
            if (ownerId == userId)
                return true;

            var members = await _sharedService.Query(new ProjectMembersQuery { ProjectId = projectId });
            return members.Members.Any(member => member.UserId == userId);
        }
    }
}
