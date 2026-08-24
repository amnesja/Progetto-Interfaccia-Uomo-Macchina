using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ordo.Services.Shared
{
    public class ProjectChatMessagesQuery
    {
        public Guid ProjectId { get; set; }
        public int Take { get; set; } = 100;
    }

    public class ProjectChatMessagesDTO
    {
        public IEnumerable<Message> Messages { get; set; }

        public class Message
        {
            public Guid Id { get; set; }
            public Guid UserId { get; set; }
            public string UserName { get; set; }
            public string Testo { get; set; }
            public DateTime DataCreazione { get; set; }
        }
    }

    public partial class SharedService
    {
        public async Task<ProjectChatMessagesDTO> Query(ProjectChatMessagesQuery qry)
        {
            var messages = await _dbContext.ProjectChatMessages
                .AsNoTracking()
                .Where(message => message.ProjectId == qry.ProjectId)
                .OrderByDescending(message => message.DataCreazione)
                .Take(qry.Take)
                .Select(message => new ProjectChatMessagesDTO.Message
                {
                    Id = message.Id,
                    UserId = message.UserId,
                    UserName = string.IsNullOrWhiteSpace(message.User.NickName)
                        ? message.User.Email
                        : message.User.NickName,
                    Testo = message.Testo,
                    DataCreazione = message.DataCreazione
                })
                .ToArrayAsync();

            return new ProjectChatMessagesDTO { Messages = messages.Reverse() };
        }
    }
}
