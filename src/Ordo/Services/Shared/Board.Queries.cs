using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ordo.Services.Shared
{
    public class BoardsByProjectQuery
    {
        public Guid ProjectId { get; set; }
    }

    public class BoardsByProjectDTO
    {
        public IEnumerable<Board> Boards { get; set; }

        public class Board
        {
            public Guid Id { get; set; }
            public string Nome { get; set; }
        }
    }

    public class BoardDetailQuery
    {
        public Guid Id { get; set; }
    }

    public class BoardDetailDTO
    {
        public Guid Id { get; set; }
        public string Nome { get; set; }
        public Guid ProjectId { get; set; }
    }

    public partial class SharedService
    {
        public async Task<BoardsByProjectDTO> Query(BoardsByProjectQuery qry)
        {
            var queryable = _dbContext.Boards
                .Where(x => x.ProjectId == qry.ProjectId);

            return new BoardsByProjectDTO
            {
                Boards = await queryable
                    .Select(x => new BoardsByProjectDTO.Board
                    {
                        Id = x.Id,
                        Nome = x.Nome
                    })
                    .ToArrayAsync()
            };
        }

        public async Task<BoardDetailDTO> Query(BoardDetailQuery qry)
        {
            return await _dbContext.Boards
                .Where(x => x.Id == qry.Id)
                .Select(x => new BoardDetailDTO
                {
                    Id = x.Id,
                    Nome = x.Nome,
                    ProjectId = x.ProjectId
                })
                .FirstOrDefaultAsync();
        }
    }
}
