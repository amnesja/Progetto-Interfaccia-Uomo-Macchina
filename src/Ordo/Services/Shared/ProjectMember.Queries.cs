using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ordo.Services.Shared
{
    public class ProjectMembersQuery
    {
        public Guid ProjectId { get; set; }
    }

    public class ProjectMembersDTO
    {
        public IEnumerable<Member> Members { get; set; }

        public class Member
        {
            public Guid UserId { get; set; }
            public string Email { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
        }
    }

    public partial class SharedService
    {
        public async Task<ProjectMembersDTO> Query(ProjectMembersQuery qry)
        {
            var queryable = _dbContext.ProjectMembers
                .Where(x => x.ProjectId == qry.ProjectId);

            return new ProjectMembersDTO
            {
                Members = await queryable
                    .Select(x => new ProjectMembersDTO.Member
                    {
                        UserId = x.UserId,
                        Email = x.User.Email,
                        FirstName = x.User.FirstName,
                        LastName = x.User.LastName
                    })
                    .ToArrayAsync()
            };
        }
    }
}