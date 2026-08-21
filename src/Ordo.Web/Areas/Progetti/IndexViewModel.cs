using Ordo.Web.Infrastructure;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Ordo.Services.Shared;
using Microsoft.AspNetCore.Mvc;

namespace Ordo.Web.Areas.Progetti
{
    public class IndexViewModel : PagingViewModel
    {
        public IndexViewModel()
        {
            OrderBy = nameof(ProjectIndexViewModel.Nome);
            OrderByDescending = false;
            Progetti = Array.Empty<ProjectIndexViewModel>();
        }

        [Display(Name = "Cerca")]
        public string Filter { get; set; }

        public IEnumerable<ProjectIndexViewModel> Progetti { get; set; }

        internal void SetProjects(ProjectsIndexDTO dto)
        {
            Progetti = dto.Projects.Select(x => new ProjectIndexViewModel(x)).ToArray();
            TotalItems = dto.Count;
        }

        public ProjectsIndexQuery ToProjectsIndexQuery(Guid idCurrentUser)
        {
            return new ProjectsIndexQuery
            {
                IdCurrentUser = idCurrentUser,
                Filter = Filter,
                Paging = new Ordo.Infrastructure.Paging
                {
                    OrderBy = OrderBy,
                    OrderByDescending = OrderByDescending,
                    Page = Page,
                    PageSize = PageSize
                }
            };
        }

        public override IActionResult GetRoute() => MVC.Progetti.Progetti.Index(this).GetAwaiter().GetResult();
    }

    public class ProjectIndexViewModel
    {
        public ProjectIndexViewModel(ProjectsIndexDTO.Project dto)
        {
            Id = dto.Id;
            Nome = dto.Nome;
            Descrizione = dto.Descrizione;
            IsOwner = dto.IsOwner;
        }

        public Guid Id { get; set; }
        public string Nome { get; set; }
        public string Descrizione { get; set; }
        public bool IsOwner { get; set; } 
    }
}