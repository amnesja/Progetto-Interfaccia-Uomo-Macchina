using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ordo.Services.Shared
{
    public class CommentsByTaskQuery
    {
        public Guid TaskId { get; set; }
    }

    public class CommentsByTaskDTO
    {
        public IEnumerable<Comment> Comments { get; set; }

        public class Comment
        {
            public Guid Id { get; set; }
            public string Testo { get; set; }
            public DateTime DataCreazione { get; set; }
            public Guid UserId { get; set; }
            public string UserNickName { get; set; }
        }
    }

    public partial class SharedService
    {
        public async Task<CommentsByTaskDTO> Query(CommentsByTaskQuery qry)
        {
            var queryable = _dbContext.Comments
                .Where(x => x.TaskId == qry.TaskId)
                .OrderBy(x => x.DataCreazione);

            return new CommentsByTaskDTO
            {
                Comments = await queryable
                    .Select(x => new CommentsByTaskDTO.Comment
                    {
                        Id = x.Id,
                        Testo = x.Testo,
                        DataCreazione = x.DataCreazione,
                        UserId = x.UserId,
                        UserNickName = x.User.NickName
                    })
                    .ToArrayAsync()
            };
        }
    }
}
