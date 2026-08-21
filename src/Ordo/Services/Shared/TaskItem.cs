using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ordo.Services.Shared
{
    public enum Priorita
    {
        Bassa = 0,
        Media = 1,
        Alta = 2
    }

    public enum TaskState
    {
        ToDo = 0,
        InProgress = 1,
        Review = 2,
        Done = 3
    }

    public class TaskItem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        public string Titolo { get; set; }
        public string Descrizione { get; set; }
        public Priorita Priorita { get; set; }
        public TaskState Stato { get; set; }
        public DateTime? Scadenza { get; set; }

        public Guid BoardId { get; set; }
        public Board Board { get; set; }

        public Guid? AssignedUserId { get; set; }
        public User AssignedUser { get; set; }

        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    }
}
