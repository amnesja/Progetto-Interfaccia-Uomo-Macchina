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
        public async Task<bool> Handle(AddProjectMemberCommand cmd)
        {
            var project = await _dbContext.Projects
                .Where(x => x.Id == cmd.ProjectId)
                .FirstOrDefaultAsync();

            if (project == null) return false;
            if (project.OwnerId == cmd.UserId) return false; // il proprietario non è un "membro"

            var giaMembro = await _dbContext.ProjectMembers
                .AnyAsync(x => x.ProjectId == cmd.ProjectId && x.UserId == cmd.UserId);

            if (giaMembro) return false;

            _dbContext.ProjectMembers.Add(new ProjectMember
            {
                ProjectId = cmd.ProjectId,
                UserId = cmd.UserId
            });

            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task Handle(RemoveProjectMemberCommand cmd)
        {
            var membro = await _dbContext.ProjectMembers
                .Where(x => x.ProjectId == cmd.ProjectId && x.UserId == cmd.UserId)
                .FirstOrDefaultAsync();

            if (membro == null) return;

            _dbContext.ProjectMembers.Remove(membro);

            await _dbContext.SaveChangesAsync();
        }
    }
}