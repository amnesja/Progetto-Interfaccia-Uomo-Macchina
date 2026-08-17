using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Ordo.Services.Shared;

namespace Ordo.Web.Areas.Progetti
{
    public class DettaglioViewModel
    {
        public Guid Id { get; set; }
        public string Nome { get; set; }
        public string Descrizione { get; set; }
        public bool IsOwner { get; set; }

        public IEnumerable<BoardItemViewModel> Boards { get; set; } = Array.Empty<BoardItemViewModel>();
        public IEnumerable<MemberItemViewModel> Membri { get; set; } = Array.Empty<MemberItemViewModel>();

        public void SetProject(ProjectDetailDTO dto, bool isOwner)
        {
            Id = dto.Id;
            Nome = dto.Nome;
            Descrizione = dto.Descrizione;
            IsOwner = isOwner;
        }

        public void SetBoards(BoardsByProjectDTO dto)
        {
            Boards = dto.Boards.Select(x => new BoardItemViewModel { Id = x.Id, Nome = x.Nome }).ToArray();
        }

        public void SetMembers(ProjectMembersDTO dto)
        {
            Membri = dto.Members.Select(x => new MemberItemViewModel
            {
                UserId = x.UserId,
                NomeCompleto = string.IsNullOrWhiteSpace(x.FirstName) ? x.Email : $"{x.FirstName} {x.LastName}",
                Email = x.Email
            }).ToArray();
        }
    }

    public class BoardItemViewModel
    {
        public Guid Id { get; set; }
        public string Nome { get; set; }
    }

    public class BoardFormViewModel
    {
        public Guid? Id { get; set; }

        [Required(ErrorMessage = "Il nome della board è obbligatorio.")]
        public string Nome { get; set; }

        public Guid ProjectId { get; set; }

        public AddOrUpdateBoardCommand ToAddOrUpdateBoardCommand()
        {
            return new AddOrUpdateBoardCommand { Id = Id, Nome = Nome, ProjectId = ProjectId };
        }
    }

    public class MemberItemViewModel
    {
        public Guid UserId { get; set; }
        public string NomeCompleto { get; set; }
        public string Email { get; set; }
    }

    public class MemberFormViewModel
    {
        [Required(ErrorMessage = "L'email è obbligatoria")]
        [EmailAddress(ErrorMessage = "Email non valida")]
        public string Email { get; set; }

        public Guid ProjectId { get; set; }
    }
}