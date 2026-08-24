using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Ordo.Web.Areas.Progetti
{
    public class ChatViewModel
    {
        public Guid ProjectId { get; set; }
        public string ProjectNome { get; set; }
        public IEnumerable<ChatMessageViewModel> Messages { get; set; } = Array.Empty<ChatMessageViewModel>();
    }

    public class ChatMessageViewModel
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; }
        public string Testo { get; set; }
        public DateTime DataCreazione { get; set; }
    }

    public class ChatMessageFormViewModel
    {
        [Required(ErrorMessage = "Scrivi un messaggio prima di inviarlo.")]
        [StringLength(2000, ErrorMessage = "Il messaggio può contenere al massimo 2000 caratteri.")]
        public string Testo { get; set; }

        public Guid ProjectId { get; set; }
    }
}
