using System;
using Ordo.Services.Shared;

namespace Ordo.Web.SignalR.Hubs.Events
{
    // IdGroup = BoardId: ogni evento viene notificato solo a chi sta guardando quella board

    public class TaskMovedEvent
    {
        public Guid IdGroup { get; set; }
        public Guid TaskId { get; set; }
        public TaskState NuovoStato { get; set; }
    }

    public class TaskCreatedEvent
    {
        public Guid IdGroup { get; set; }
        public Guid TaskId { get; set; }
    }

    public class CommentAddedEvent
    {
        public Guid IdGroup { get; set; }
        public Guid TaskId { get; set; }
        public Guid CommentId { get; set; }
    }

    public class UserAssignedEvent
    {
        public Guid IdGroup { get; set; }
        public Guid TaskId { get; set; }
        public Guid UserId { get; set; }
    }
}