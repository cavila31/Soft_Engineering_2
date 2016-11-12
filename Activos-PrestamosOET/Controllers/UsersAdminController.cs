using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.AspNet.Identity.EntityFramework;
using System.Data.Entity;
using System.Net;
using System.Threading.Tasks;
using System.Web.Mvc;
using Activos_PrestamosOET.Models;
using PagedList;

namespace Activos_PrestamosOET.Controllers
{
    [Authorize(Roles = "superadmin")]
    public class UsersAdminController : Controller
    {

        private PrestamosEntities db = new PrestamosEntities();

        public UsersAdminController()
        {
        }

        public UsersAdminController(ApplicationUserManager userManager, ApplicationRoleManager roleManager)
        {
            UserManager = userManager;
            RoleManager = roleManager;
        }

        private ApplicationUserManager _userManager;
        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }

        private ApplicationRoleManager _roleManager;
        public ApplicationRoleManager RoleManager
        {
            get
            {
                return _roleManager ?? HttpContext.GetOwinContext().Get<ApplicationRoleManager>();
            }
            private set
            {
                _roleManager = value;
            }
        }

        //
        // GET: /Users/
        public async Task<ActionResult> Index(string orden, int? pagina, string busqueda)
        {
            ViewBag.OrdenActual = orden;
            ViewBag.Nombre = String.IsNullOrEmpty(orden) ? "nombre_desc" : "";
            ViewBag.Correo = (orden == "correo_asc") ? "correo_desc" : "correo_asc";

            var usuarios = await UserManager.Users.ToListAsync();

            #region Busqueda de usuarios
            if (!String.IsNullOrEmpty(busqueda))
                usuarios = usuarios.Where(usr => usr.FullName.Contains(busqueda) || usr.Email.Contains(busqueda)).ToList();
            #endregion

            switch (orden)
            {
                case "nombre_desc":
                    usuarios = usuarios.OrderByDescending(emp => emp.FullName).ToList();
                    break;
                case "correo_asc":
                    usuarios = usuarios.OrderBy(emp => emp.Email).ToList();
                    break;
                case "correo_desc":
                    usuarios = usuarios.OrderByDescending(emp => emp.Email).ToList();
                    break;
                default:
                    usuarios = usuarios.OrderBy(emp => emp.FullName).ToList();
                    break;
            }
            int tamano_pagina = 20;
            int num_pagina = (pagina ?? 1);
            return View(usuarios.ToPagedList(num_pagina, tamano_pagina));
        }

        public struct TransaccionesConActivo
        {
            public string activo_id;
            public DateTime transaccion_fecha;
            public string activo_descripcion;
            public string numero_de_placa;
            public string transaccion_descripcion;
            public string activo_nuevo_estado;
            public string activo_categoria;
            public bool activo_desechado;
            public TransaccionesConActivo(string id, DateTime fecha, string descripcion, string trans_desc, string placa, bool desechado, string nuevo_estado, string act_cat)
            {
                activo_id = id;
                transaccion_fecha = fecha;
                activo_descripcion = descripcion;
                transaccion_descripcion = trans_desc;
                numero_de_placa = placa;
                activo_desechado = desechado;
                activo_nuevo_estado = nuevo_estado;
                activo_categoria = act_cat;
            }
        }
        //
        // GET: /Users/Details/5
        public async Task<ActionResult> Details(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var user = await UserManager.FindByIdAsync(id);

            ViewBag.RoleNames = await UserManager.GetRolesAsync(user.Id);
            var estaciones = db.V_ESTACION.ToList();
            ViewBag.Estacion = estaciones.Where(e => e.ID.Equals(user.EstacionID)).ToList()[0].NOMBRE;
            List<TRANSACCION> listaTransacciones = db.TRANSACCIONES.Where(e => e.RESPONSABLE.Equals(user.Email)).ToList();
            List<TransaccionesConActivo> transacciones_listas = new List<TransaccionesConActivo>();
            foreach(TRANSACCION item in listaTransacciones)
            {
                ACTIVO activo = db.ACTIVOS.Find(item.ACTIVOID);
                TIPOS_ACTIVOS tipo = db.TIPOS_ACTIVOS.Find(activo.TIPO_ACTIVOID);
                transacciones_listas.Add(new TransaccionesConActivo(item.ACTIVOID, item.FECHA, activo.DESCRIPCION, item.DESCRIPCION, activo.PLACA, activo.DESECHADO, item.ESTADO, tipo.NOMBRE));
            }
            ViewBag.Transacciones = transacciones_listas;

            return View(user);
        }

        //
        // GET: /Users/Create
        public async Task<ActionResult> Create()
        {
            //Get the list of Roles
            ViewBag.RoleId = new SelectList(await RoleManager.Roles.ToListAsync(), "Name", "Name");
            ViewBag.EstacionID = new SelectList(db.V_ESTACION, "ID", "NOMBRE");
            return View();
        }

        //
        // POST: /Users/Create
        [HttpPost]
        public async Task<ActionResult> Create(RegisterViewModel userViewModel, params string[] selectedRoles)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = userViewModel.Email,
                    Email = userViewModel.Email,
                    // Add the Address Info:
                    Nombre = userViewModel.Nombre,
                    Apellidos = userViewModel.Apellidos,
                    Cedula = userViewModel.Cedula,
                    EstacionID = userViewModel.EstacionID
                };



                // Then create:
                var adminresult = await UserManager.CreateAsync(user, userViewModel.Password);

                //Add User to the selected Roles
                if (adminresult.Succeeded)
                {
                    if (selectedRoles != null)
                    {
                        var result = await UserManager.AddToRolesAsync(user.Id, selectedRoles);
                        if (!result.Succeeded)
                        {
                            ModelState.AddModelError("", result.Errors.First());
                            ViewBag.EstacionID = new SelectList(db.V_ESTACION, "ID", "NOMBRE");
                            ViewBag.RoleId = new SelectList(await RoleManager.Roles.ToListAsync(), "Name", "Name");
                            return View();
                        }
                    }


                    //enviar correo de confirmacion al nuevo usuario
                    string code = await UserManager.GenerateEmailConfirmationTokenAsync(user.Id);
                    var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);
                    string cuerpo_del_mensaje = "Bienvenido al sistema de Activos de la Organizacicón de Estudios Tropicales. " +
                                                "Se creó una cuenta asociada a este correo con los siguientes roles: Préstamos -" + string.Join(" - ", selectedRoles)+". " +
                                                "Por favor confirme su correo ingresando a este <a href=\"" + callbackUrl + "\">enlace</a>. " +
                                                "Si no solicitó una cuenta por favor ignore este correo.";

                    await UserManager.SendEmailAsync(user.Id, "Confirme su inscripción", cuerpo_del_mensaje);

                    ViewBag.Message = "Se ha enviado un correo de confirmacion a la direccion del nuevo usuario. El nuevo usuario debe confirmar su dirección de correo" +
                                        " para poder ingresar al sistema.";

                    return View("Info");
                }
                else
                {
                    ModelState.AddModelError("", adminresult.Errors.First());
                    ViewBag.RoleId = new SelectList(RoleManager.Roles, "Name", "Name");
                    ViewBag.EstacionID = new SelectList(db.V_ESTACION, "ID", "NOMBRE");
                    return View();

                }
                //return RedirectToAction("Index");
            }
            ViewBag.EstacionID = new SelectList(db.V_ESTACION, "ID", "NOMBRE");
            ViewBag.RoleId = new SelectList(RoleManager.Roles, "Name", "Name");
            return View();
        }


        //
        // GET: /Users/Edit/1
        public async Task<ActionResult> Edit(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var user = await UserManager.FindByIdAsync(id);
            if (user == null)
            {
                return HttpNotFound();
            }

            var userRoles = await UserManager.GetRolesAsync(user.Id);
            var editUser = new EditUserViewModel();
            editUser.Id = user.Id;
            editUser.Email = user.Email;
            editUser.Nombre = user.Nombre;
            editUser.Apellidos = user.Apellidos;
            editUser.Cedula = user.Cedula;
            editUser.EstacionID = user.EstacionID;
            editUser.RolesList = RoleManager.Roles.ToList().Select(x => new SelectListItem()
            {
                Selected = userRoles.Contains(x.Name),
                Text = x.Name,
                Value = x.Name
            });
            ViewBag.EstacionID = new SelectList(db.V_ESTACION, "ID", "NOMBRE", editUser.EstacionID);
            return View(editUser);
        }

        //
        // POST: /Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "Email,Id,Nombre,Apellidos,Cedula,EstacionID")] EditUserViewModel editUser, params string[] SelectedRoles)
        {
            if (ModelState.IsValid)
            {
                var user = await UserManager.FindByIdAsync(editUser.Id);
                if (user == null)
                {
                    return HttpNotFound();
                }

                user.UserName = editUser.Email;
                user.Email = editUser.Email;
                user.Nombre = editUser.Nombre;
                user.Apellidos = editUser.Apellidos;
                user.Cedula = editUser.Cedula;
                user.EstacionID = editUser.EstacionID;

                var userRoles = await UserManager.GetRolesAsync(user.Id);

                string[] selectedRoles = SelectedRoles;

                SelectedRoles = SelectedRoles ?? new string[] { };

                var result = await UserManager.AddToRolesAsync(user.Id, SelectedRoles.Except(userRoles).ToArray<string>());

                if (!result.Succeeded)
                {
                    ModelState.AddModelError("", result.Errors.First());
                    return View();
                }
                result = await UserManager.RemoveFromRolesAsync(user.Id, userRoles.Except(SelectedRoles).ToArray<string>());

                if (!result.Succeeded)
                {
                    ModelState.AddModelError("", result.Errors.First());
                    return View();
                }

                //Si los roles asignados cambiaron
                if (SelectedRoles.Length != userRoles.Count)
                {
                    //enviar correo con cambios al usuario
                    string cuerpo_del_mensaje = "Este correo es para informarle que sus roles dentro del sistema de Administración de Activos fueron cambiados. " +
                                                "Sus roles actuales son los siguientes: Préstamos -" + string.Join(" - ", selectedRoles) + ". " +
                                                "Si usted no posee una cuenta en el sistema por favor ignore este correo.";

                    await UserManager.SendEmailAsync(user.Id, "Sus roles en el sistema han sido cambiados.", cuerpo_del_mensaje);
                }

                return RedirectToAction("Index");
            }
            ModelState.AddModelError("", "Something failed.");
            return View();
        }

        //
        // GET: /Users/Delete/5
        public async Task<ActionResult> Delete(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var user = await UserManager.FindByIdAsync(id);
            if (user == null)
            {
                return HttpNotFound();
            }
            ViewBag.RoleNames = await UserManager.GetRolesAsync(user.Id);
            var estaciones = db.V_ESTACION.ToList();
            ViewBag.Estacion = estaciones.Where(e => e.ID.Equals(user.EstacionID)).ToList()[0].NOMBRE;
            return View(user);
        }

        //
        // POST: /Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(string id)
        {
            var user = await UserManager.FindByIdAsync(id);
            if (ModelState.IsValid)
            {
                if (id == null)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                }

                if (user == null)
                {
                    return HttpNotFound();
                }
                var result = await UserManager.DeleteAsync(user);
                if (!result.Succeeded)
                {
                    ModelState.AddModelError("", result.Errors.First());
                    return View();
                }
                return RedirectToAction("Index");
            }
            ViewBag.RoleNames = await UserManager.GetRolesAsync(user.Id);
            var estaciones = db.V_ESTACION.ToList();
            ViewBag.Estacion = estaciones.Where(e => e.ID.Equals(user.EstacionID)).ToList()[0].NOMBRE;
            return View();
        }
    }
}