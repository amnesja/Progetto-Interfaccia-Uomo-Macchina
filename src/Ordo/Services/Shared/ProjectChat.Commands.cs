using System;
using System.Threading.Tasks;

namespace Ordo.Services.Shared
{
    public class AddProjectChatMessageCommand
    {
        public Guid ProjectId { get; set; }
        public Guid UserId { get; set; }
        public string Testo { get; set; }
    }

    public partial class SharedService
    {
        public async Task<ProjectChatMessage> Handle(AddProjectChatMessageCommand cmd)
        {
            var message = new ProjectChatMessage
            {
                ProjectId = cmd.ProjectId,
                UserId = cmd.UserId,
                Testo = cmd.Testo.Trim(),
                DataCreazione = DateTime.UtcNow
            };

            _dbContext.ProjectChatMessages.Add(message);
            await _dbContext.SaveChangesAsync();
            return message;
        }
    }
}
