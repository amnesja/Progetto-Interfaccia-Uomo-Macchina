using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ordo.Infrastructure;

namespace Ordo.Services.Shared
{
    public class ProjectsIndexQuery
    {
        public Guid IdCurrentUser { get; set; }
        public string Filter { get; set; }

        public Paging Paging { get; set; }
    }

    public class ProjectsIndexDTO
    {
        public IEnumerable<Project> Projects { get; set; }
        public int Count { get; set; }

        public class Project
        {
            public Guid Id { get; set; }
            public string Nome { get; set; }
            public string Descrizione { get; set; }
            public Guid OwnerId { get; set; }
        }
    }

    public class ProjectDetailQuery
    {
        public Guid Id { get; set; }
    }

    public class ProjectDetailDTO
    {
        public Guid Id { get; set; }
        public string Nome { get; set; }
        public string Descrizione { get; set; }
        public Guid OwnerId { get; set; }
    }

    public partial class SharedService
    {
        /// <summary>
        /// Restituisce i progetti di cui l'utente corrente e' owner (o membro, se in futuro aggiungerete la tabella dei membri)
        /// </summary>
        public async Task<ProjectsIndexDTO> Query(ProjectsIndexQuery qry)
        {
            var queryable = _dbContext.Projects
                .Where(x => x.OwnerId == qry.IdCurrentUser);

            if (string.IsNullOrWhiteSpace(qry.Filter) == false)
            {
                queryable = queryable.Where(x => x.Nome.Contains(qry.Filter));
            }

            return new ProjectsIndexDTO
            {
                Projects = await queryable
                    .ApplyPaging(qry.Paging)
                    .Select(x => new ProjectsIndexDTO.Project
                    {
                        Id = x.Id,
                        Nome = x.Nome,
                        Descrizione = x.Descrizione,
                        OwnerId = x.OwnerId
                    })
                    .ToArrayAsync(),
                Count = await queryable.CountAsync()
            };
        }

        public async Task<ProjectDetailDTO> Query(ProjectDetailQuery qry)
        {
            return await _dbContext.Projects
                .Where(x => x.Id == qry.Id)
                .Select(x => new ProjectDetailDTO
                {
                    Id = x.Id,
                    Nome = x.Nome,
                    Descrizione = x.Descrizione,
                    OwnerId = x.OwnerId
                })
                .FirstOrDefaultAsync();
        }
    }
}
