using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using System.ComponentModel.DataAnnotations.Schema;

namespace Activos_PrestamosOET.Models
{
    public class RoleViewModel
    {
        public string Id { get; set; }
        [Required(AllowEmptyStrings = false, ErrorMessage = "El nombre es obligatorio")]
        [Display(Name = "Nombre de rol")]
        public string Name { get; set; }
    }

    [NotMapped]
    public class EditUserViewModel
    {
        public string Id { get; set; }

        [Required(AllowEmptyStrings = false, ErrorMessage = "El correo es obligatorio" )]
        [Display(Name = "Correo")]
        [EmailAddress]
        public string Email { get; set; }

        // Info personal:
        [Display(Name ="Nombre")]
        public string Nombre { get; set; }
        [Display(Name ="Apellidos")]
        public string Apellidos { get; set; }
        [Display(Name = "Cédula")]
        public string Cedula { get; set; }

        [Display(Name = "Estación")]
        public string EstacionID { get; set; }


        public IEnumerable<SelectListItem> RolesList { get; set; }
       
    }
}
