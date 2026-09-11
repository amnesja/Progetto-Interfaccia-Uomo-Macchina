using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Ordo.Services.Shared;
using Ordo.Web.Infrastructure;

namespace Ordo.Web.Areas.Progetti
{
    public class DettaglioViewModel
    {
        public Guid Id { get; set; }
        public Guid OwnerId { get; set; }
        public string Nome { get; set; }
        public string Descrizione { get; set; }
        public bool IsOwner { get; set; }

        public IEnumerable<BoardItemViewModel> Boards { get; set; } = Array.Empty<BoardItemViewModel>();
        public IEnumerable<MemberItemViewModel> Membri { get; set; } = Array.Empty<MemberItemViewModel>();
        public IEnumerable<ChatMessageViewModel> Messages { get; set; } = Array.Empty<ChatMessageViewModel>();

        public void SetProject(ProjectDetailDTO dto, bool isOwner)
        {
            Id = dto.Id;
            OwnerId = dto.OwnerId;
            Nome = dto.Nome;
            Descrizione = dto.Descrizione;
            IsOwner = isOwner;
        }

        public void SetBoards(BoardsByProjectDTO dto)
        {
            Boards = dto.Boards.Select(x => new BoardItemViewModel { Id = x.Id, Nome = x.Nome }).ToArray();
        }

        public void SetBoardTasks(IEnumerable<BoardTaskViewModel> tasks)
        {
            var taskLookup = tasks.GroupBy(x => x.BoardId)
                .ToDictionary(group => group.Key, group => group.ToArray());

            foreach (var board in Boards)
                board.Tasks = taskLookup.TryGetValue(board.Id, out var boardTasks)
                    ? boardTasks
                    : Array.Empty<BoardTaskViewModel>();
        }

        public void SetMessages(ProjectChatMessagesDTO dto)
        {
            Messages = dto.Messages.Select(message => new ChatMessageViewModel
            {
                Id = message.Id,
                UserId = message.UserId,
                UserName = message.UserName,
                Testo = message.Testo,
                DataCreazione = message.DataCreazione
            }).ToArray();
        }

        public string ToJson() => JsonSerializer.ToJsonCamelCase(this);

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
        public IEnumerable<BoardTaskViewModel> Tasks { get; set; } = Array.Empty<BoardTaskViewModel>();
    }

    public class BoardTaskViewModel
    {
        public Guid Id { get; set; }
        public Guid BoardId { get; set; }
        public string Titolo { get; set; }
        public string Descrizione { get; set; }
        public Priorita Priorita { get; set; }
        public TaskState Stato { get; set; }
        public DateTime? Scadenza { get; set; }
        public Guid? AssignedUserId { get; set; }
        public string AssignedUserName { get; set; }
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