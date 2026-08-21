using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ordo.Services.Shared
{
    public class Comment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        public string Testo { get; set; }
        public DateTime DataCreazione { get; set; }

        public Guid TaskId { get; set; }
        public TaskItem Task { get; set; }

        public Guid UserId { get; set; }
        public User User { get; set; }
    }
}
