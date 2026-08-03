using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Ordo.Services.Shared
{
    public class AddOrUpdateBoardCommand
    {
        public Guid? Id { get; set; }
        public string Nome { get; set; }
        public Guid ProjectId { get; set; }
    }

    public class DeleteBoardCommand
    {
        public Guid Id { get; set; }
    }

    public partial class SharedService
    {
        public async Task<Guid> Handle(AddOrUpdateBoardCommand cmd)
        {
            var board = await _dbContext.Boards
                .Where(x => x.Id == cmd.Id)
                .FirstOrDefaultAsync();

            if (board == null)
            {
                board = new Board
                {
                    ProjectId = cmd.ProjectId,
                };
                _dbContext.Boards.Add(board);
            }

            board.Nome = cmd.Nome;

            await _dbContext.SaveChangesAsync();

            return board.Id;
        }

        public async Task Handle(DeleteBoardCommand cmd)
        {
            var board = await _dbContext.Boards
                .Where(x => x.Id == cmd.Id)
                .FirstOrDefaultAsync();

            if (board == null) return;

            _dbContext.Boards.Remove(board);

            await _dbContext.SaveChangesAsync();
        }
    }
}
