using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ordo.Services.Shared
{
    public class Project
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        public string Nome { get; set; }
        public string Descrizione { get; set; }

        public Guid OwnerId { get; set; }
        public User Owner { get; set; }

        public ICollection<Board> Boards { get; set; } = new List<Board>();
    }
}
