using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Ordo.Services.Shared
{
    public class AddOrUpdateProjectCommand
    {
        public Guid? Id { get; set; }
        public string Nome { get; set; }
        public string Descrizione { get; set; }
        public Guid OwnerId { get; set; }
    }

    public class DeleteProjectCommand
    {
        public Guid Id { get; set; }
    }

    public partial class SharedService
    {
        public async Task<Guid> Handle(AddOrUpdateProjectCommand cmd)
        {
            var project = await _dbContext.Projects
                .Where(x => x.Id == cmd.Id)
                .FirstOrDefaultAsync();

            if (project == null)
            {
                project = new Project
                {
                    OwnerId = cmd.OwnerId,
                };
                _dbContext.Projects.Add(project);
            }

            project.Nome = cmd.Nome;
            project.Descrizione = cmd.Descrizione;

            await _dbContext.SaveChangesAsync();

            return project.Id;
        }

        public async Task Handle(DeleteProjectCommand cmd)
        {
            var project = await _dbContext.Projects
                .Where(x => x.Id == cmd.Id)
                .FirstOrDefaultAsync();

            if (project == null) return;

            _dbContext.Projects.Remove(project);

            await _dbContext.SaveChangesAsync();
        }
    }
}
