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

        public Task When(CommentDeletedEvent e)
        {
            return GetOrdoGroup(e.IdGroup).CommentDeleted(e);
        }

        public Task When(UserAssignedEvent e)
        {
            return GetOrdoGroup(e.IdGroup).UserAssigned(e.TaskId, e.UserId, e.AssignedUserName, e.Titolo);
        }

        public Task When(MemberAddedEvent e)
        {
            return GetOrdoGroup(e.IdGroup).ProjectMemberAdded(e.ProjectId, e.ProjectNome, e.ProjectDescrizione);
        }

        public Task When(ProjectDeletedEvent e)
        {
            var tasks = new System.Collections.Generic.List<Task>
            {
                GetOrdoGroup(e.ProjectId).ProjectDeleted(e.ProjectId)
            };

            foreach (var userId in e.UtentiCoinvolti ?? Array.Empty<Guid>())
            {
                tasks.Add(GetOrdoGroup(userId).ProjectDeleted(e.ProjectId));
            }

            return Task.WhenAll(tasks);
        }

        public Task When(BoardCreatedEvent e)
        {
            var tasks = new System.Collections.Generic.List<Task>
            {
                GetOrdoGroup(e.ProjectId).BoardCreated(e.ProjectId, e.BoardId, e.BoardNome)
            };
            foreach (var userId in e.UtentiCoinvolti ?? Array.Empty<Guid>())
                tasks.Add(GetOrdoGroup(userId).BoardCreated(e.ProjectId, e.BoardId, e.BoardNome));
            return Task.WhenAll(tasks);
        }

        public Task When(BoardDeletedEvent e)
        {
            var tasks = new System.Collections.Generic.List<Task>
            {
                GetOrdoGroup(e.ProjectId).BoardDeleted(e.ProjectId, e.BoardId, e.BoardNome),
                GetOrdoGroup(e.BoardId).BoardDeleted(e.ProjectId, e.BoardId, e.BoardNome)
            };
            foreach (var userId in e.UtentiCoinvolti ?? Array.Empty<Guid>())
                tasks.Add(GetOrdoGroup(userId).BoardDeleted(e.ProjectId, e.BoardId, e.BoardNome));
            return Task.WhenAll(tasks);
        }

        public Task When(ProjectUpdatedEvent e)
        {
            var tasks = new System.Collections.Generic.List<Task>
            {
                GetOrdoGroup(e.ProjectId).ProjectUpdated(e.ProjectId, e.Nome, e.Descrizione)
            };

            foreach (var userId in e.UtentiCoinvolti ?? Array.Empty<Guid>())
            {
                tasks.Add(GetOrdoGroup(userId).ProjectUpdated(e.ProjectId, e.Nome, e.Descrizione));
            }

            return Task.WhenAll(tasks);
        }

        public Task When(BoardUpdatedEvent e)
        {
            var tasks = new System.Collections.Generic.List<Task>
            {
                GetOrdoGroup(e.ProjectId).BoardUpdated(e.ProjectId, e.BoardId, e.BoardNome),
                GetOrdoGroup(e.BoardId).BoardUpdated(e.ProjectId, e.BoardId, e.BoardNome)
            };
            foreach (var userId in e.UtentiCoinvolti ?? Array.Empty<Guid>())
                tasks.Add(GetOrdoGroup(userId).BoardUpdated(e.ProjectId, e.BoardId, e.BoardNome));
            return Task.WhenAll(tasks);
        }

        public Task When(TaskChangedForUserEvent e)
        {
            return GetOrdoGroup(e.IdGroup).TaskChangedForUser(e.Tipo, e.Titolo, e.ProjectNome, e.ProjectId, e.BoardId, e.TaskId);
        }

        public Task When(MemberRemovedEvent e)
        {
            return Task.WhenAll(
                GetOrdoGroup(e.UserId).ProjectDeleted(e.ProjectId),
                GetOrdoGroup(e.ProjectId).MemberRemoved(e.ProjectId, e.UserId)
            );
        }

        public Task When(ProjectChatMessageEvent e)
        {
            var tasks = new System.Collections.Generic.List<Task>
            {
                GetOrdoGroup(e.IdGroup).ProjectChatMessageAdded(e)
            };

            foreach (var userId in e.UtentiCoinvolti ?? Array.Empty<Guid>())
            {
                if (userId != e.UserId)
                    tasks.Add(GetOrdoGroup(userId).ProjectChatMessageAdded(e));
            }

            return Task.WhenAll(tasks);
        }
    }
}
