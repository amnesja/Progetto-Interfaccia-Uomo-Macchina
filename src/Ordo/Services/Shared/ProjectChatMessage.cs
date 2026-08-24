using System;

namespace Ordo.Services.Shared
{
    public class ProjectChatMessage
    {
        public Guid Id { get; set; }
        public Guid ProjectId { get; set; }
        public Guid UserId { get; set; }
        public string Testo { get; set; }
        public DateTime DataCreazione { get; set; }

        public Project Project { get; set; }
        public User User { get; set; }
    }
}
