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
        Task UserAssigned(Guid taskId, Guid? userId, string assignedUserName, string titolo);
    }

    [Microsoft.AspNetCore.Authorization.Authorize] // Hub utilizzabile solo da utenti autenticati
    public class OrdoHub : Hub<IOrdoClientEvent>
    {
        private readonly SharedService _sharedService;

        public OrdoHub(SharedService sharedService)
        {
            _sharedService = sharedService;
        }

        // Chiamato dal client quando entra nella pagina di una Board (vedi Board.cshtml)
        public async Task JoinGroup(Guid idGroup)
        {
            if (!await HasBoardAccess(idGroup))
                throw new HubException("Non sei autorizzato ad accedere a questa board.");

            await Groups.AddToGroupAsync(Context.ConnectionId, idGroup.ToString());
        }

        public async Task LeaveGroup(Guid idGroup)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, idGroup.ToString());
        }

        private async Task<bool> HasBoardAccess(Guid boardId)
        {
            var userIdValue = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdValue, out var userId))
                return false;

            var board = await _sharedService.Query(new BoardDetailQuery { Id = boardId });
            if (board == null)
                return false;

            var project = await _sharedService.Query(new ProjectDetailQuery { Id = board.ProjectId });
            if (project == null)
                return false;

            if (project.OwnerId == userId)
                return true;

            var members = await _sharedService.Query(new ProjectMembersQuery { ProjectId = project.Id });
            return members.Members.Any(member => member.UserId == userId);
        }
    }
}
