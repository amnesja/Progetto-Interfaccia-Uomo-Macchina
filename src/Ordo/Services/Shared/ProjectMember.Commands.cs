using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Ordo.Services.Shared
{
    public class AddProjectMemberCommand
    {
        public Guid ProjectId { get; set; }
        public Guid UserId { get; set; }
    }

    public class RemoveProjectMemberCommand
    {
        public Guid ProjectId { get; set; }
        public Guid UserId { get; set; }
    }

    public partial class SharedService
    {
        public async Task Handle(AddProjectMemberCommand cmd)
        {
            var project = await _dbContext.Projects
                .Where(x => x.Id == cmd.ProjectId)
                .FirstOrDefaultAsync();

            if (project == null) return;
            if (project.OwnerId == cmd.UserId) return; // il proprietario non è un "membro"

            var giaMembro = await _dbContext.ProjectMembers
                .AnyAsync(x => x.ProjectId == cmd.ProjectId && x.UserId == cmd.UserId);

            if (giaMembro) return;

            _dbContext.ProjectMembers.Add(new ProjectMember
            {
                ProjectId = cmd.ProjectId,
                UserId = cmd.UserId
            });

            await _dbContext.SaveChangesAsync();
        }

        public async Task Handle(RemoveProjectMemberCommand cmd)
        {
            var membro = await _dbContext.ProjectMembers
                .Where(x => x.ProjectId == cmd.ProjectId && x.UserId == cmd.UserId)
                .FirstOrDefaultAsync();

            if (membro == null) return;

            _dbContext.ProjectMembers.Remove(membro);

            // Rimuovi l'assegnazione dai task del progetto che erano assegnati a questo utente:
            // altrimenti continuerebbe a vederli in Dashboard/Attività pur non essendo più nel progetto
            var taskDaSassegnare = await _dbContext.Tasks
                .Where(t => t.Board.ProjectId == cmd.ProjectId && t.AssignedUserId == cmd.UserId)
                .ToListAsync();

            foreach (var task in taskDaSassegnare)
            {
                task.AssignedUserId = null;
            }

            await _dbContext.SaveChangesAsync();
        }
    }
}