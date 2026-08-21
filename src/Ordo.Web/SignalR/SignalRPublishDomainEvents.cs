using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;
using Ordo.Web.SignalR.Hubs;
using Ordo.Web.SignalR.Hubs.Events;

namespace Ordo.Web.SignalR
{
    public class SignalrPublishDomainEvents : IPublishDomainEvents
    {
        IHubContext<OrdoHub, IOrdoClientEvent> _OrdoHub;

        public SignalrPublishDomainEvents(IHubContext<OrdoHub, IOrdoClientEvent> OrdoHub)
        {
            _OrdoHub = OrdoHub;
        }

        private IOrdoClientEvent GetOrdoGroup(Guid id)
        {
            return _OrdoHub.Clients.Group(id.ToString());
        }

        public Task Publish(object evnt)
        {
            try
            {
                return ((dynamic)this).When((dynamic)evnt);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException)
            {
                return Task.CompletedTask;
            }
        }

        public Task When(TaskMovedEvent e)
        {
            return GetOrdoGroup(e.IdGroup).TaskMoved(e.TaskId, (int)e.NuovoStato, e.Titolo);
        }

        public Task When(TaskCreatedEvent e)
        {
            return GetOrdoGroup(e.IdGroup).TaskCreated(e);
        }

        public Task When(TaskUpdatedEvent e)
        {
            return GetOrdoGroup(e.Task.IdGroup).TaskUpdated(e);
        }

        public Task When(TaskDeletedEvent e)
        {
            return GetOrdoGroup(e.IdGroup).TaskDeleted(e);
        }

        public Task When(CommentAddedEvent e)
        {
            return GetOrdoGroup(e.IdGroup).CommentAdded(e);
        }

        public Task When(UserAssignedEvent e)
        {
            return GetOrdoGroup(e.IdGroup).UserAssigned(e.TaskId, e.UserId, e.AssignedUserName, e.Titolo);
        }
    }
}
