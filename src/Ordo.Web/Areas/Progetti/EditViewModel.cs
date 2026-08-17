using System;
using System.ComponentModel.DataAnnotations;
using Ordo.Services.Shared;

namespace Ordo.Web.Areas.Progetti
{
    public class EditViewModel
    {
        public Guid? Id { get; set; }

        [Required(ErrorMessage = "Il nome del progetto è obbligatorio.")]
        [Display(Name = "Nome progetto")]
        public string Nome { get; set; }

        [Display(Name = "Descrizione")]
        public string Descrizione { get; set; }

        public void SetProject(ProjectDetailDTO dto)
        {
            if (dto != null)
            {
                Id = dto.Id;
                Nome = dto.Nome;
                Descrizione = dto.Descrizione;
            }
        }

        public AddOrUpdateProjectCommand ToAddOrUpdateProjectCommand(Guid ownerId)
        {
            return new AddOrUpdateProjectCommand
            {
                Id = Id,
                Nome = Nome,
                Descrizione = Descrizione,
                OwnerId = ownerId
            };
        }
    }
}