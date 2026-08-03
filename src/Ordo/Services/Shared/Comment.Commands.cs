using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Ordo.Services.Shared
{
    public class AddCommentCommand
    {
        public string Testo { get; set; }
        public Guid TaskId { get; set; }
        public Guid UserId { get; set; }
    }

    public class DeleteCommentCommand
    {
        public Guid Id { get; set; }
    }

    public partial class SharedService
    {
        public async Task<Guid> Handle(AddCommentCommand cmd)
        {
            var comment = new Comment
            {
                Testo = cmd.Testo,
                TaskId = cmd.TaskId,
                UserId = cmd.UserId,
                DataCreazione = DateTime.UtcNow,
            };

            _dbContext.Comments.Add(comment);

            await _dbContext.SaveChangesAsync();

            return comment.Id;
        }

        public async Task Handle(DeleteCommentCommand cmd)
        {
            var comment = await _dbContext.Comments
                .Where(x => x.Id == cmd.Id)
                .FirstOrDefaultAsync();

            if (comment == null) return;

            _dbContext.Comments.Remove(comment);

            await _dbContext.SaveChangesAsync();
        }
    }
}
