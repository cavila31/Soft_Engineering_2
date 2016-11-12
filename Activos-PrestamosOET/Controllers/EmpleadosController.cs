using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Activos_PrestamosOET.Models;
using PagedList;

namespace Activos_PrestamosOET.Controllers
{
    [Authorize(Roles = "superadmin")]
    public class EmpleadosController : Controller
    {
        private PrestamosEntities db = new PrestamosEntities();

        // GET: Empleados
        public ActionResult Index(string orden, int? pagina, string busqueda)
        {
            ViewBag.OrdenActual = orden;
            ViewBag.Nombre = String.IsNullOrEmpty(orden) ? "nombre_desc" : "";
            ViewBag.Correo = (orden == "correo_asc") ? "correo_desc" : "correo_asc";

            var empleados = db.V_EMPLEADOS.Where(emp => emp.ESTADO.Equals(1) && emp.EMAIL.Contains("@"));

            #region Busqueda de empleados
            if (!String.IsNullOrEmpty(busqueda))
                empleados = empleados.Where(emp => emp.NOMBRE.Contains(busqueda) || emp.EMAIL.Contains(busqueda));
            #endregion

            switch (orden)
            {
                case "nombre_desc":
                    empleados = empleados.OrderByDescending(emp => emp.NOMBRE);
                    break;
                case "correo_asc":
                    empleados = empleados.OrderBy(emp => emp.EMAIL);
                    break;
                case "correo_desc":
                    empleados = empleados.OrderByDescending(emp => emp.EMAIL);
                    break;
                default:
                    empleados = empleados.OrderBy(emp => emp.NOMBRE);
                    break;
            }
            int tamano_pagina = 20;
            int num_pagina = (pagina ?? 1);
            return View(empleados.ToPagedList(num_pagina, tamano_pagina));
        }
        public struct ActivosAsignados
        {
            public string activo_id;
            public DateTime transaccion_fecha;
            public string activo_descripcion;
            public string activo_modelo;
            public string numero_de_placa;
            public bool activo_desechado;
            public ActivosAsignados(string id, DateTime fecha, string descripcion, string modelo, string placa, bool desechado)
            {
                activo_id = id;
                transaccion_fecha = fecha;
                activo_descripcion = descripcion;
                activo_modelo = modelo;
                numero_de_placa = placa;
                activo_desechado = desechado;
            }
        }
        // GET: Empleados/Details/5
        public ActionResult Details(string id)
        {

            // TODO: mostrar los activos que ha tenido asignado el usuario consultado
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            V_EMPLEADOS v_EMPLEADOS = db.V_EMPLEADOS.Find(id);
            List<ActivosAsignados> activos_asignados = new List<ActivosAsignados>();
            List<TRANSACCION> transacciones = db.TRANSACCIONES.Where(em => em.V_EMPLEADOSIDEMPLEADO.Equals(id)).ToList();

            foreach (var item in transacciones)
            {
                ACTIVO activo = db.ACTIVOS.Find(item.ACTIVOID);
                activos_asignados.Add(new ActivosAsignados(activo.ID, item.FECHA, activo.DESCRIPCION, activo.MODELO, activo.PLACA, activo.DESECHADO));
            }
            activos_asignados.OrderBy(f => f.transaccion_fecha);
            ViewBag.activos_asignados = activos_asignados;

            if (v_EMPLEADOS == null)
            {
                return HttpNotFound();
            }
            return View(v_EMPLEADOS);
        }

        /**
         * Metodo que filtra los empleados con base en una estacion.
         * @param: El identificador de la estacion
         * @return: Los usuarios que pertenecen a la estacion seleccionada, que estan activos en la empresa y que poseen correo electronico.
         **/
        public static IQueryable<V_EMPLEADOS> EmpleadosFiltrados(string id_estacion)
        {
            PrestamosEntities bd = new PrestamosEntities();
            var empleados = bd.V_EMPLEADOS.Where(emp => emp.ESTACION_ID.Equals(id_estacion) && emp.ESTADO.Equals(1) && emp.EMAIL.Contains("@")).OrderBy(emp => emp.NOMBRE).ToList().Select(empleado => new V_EMPLEADOS
            {
                IDEMPLEADO = empleado.IDEMPLEADO,
                NOMBRE = empleado.NOMBRE
            }).AsQueryable();
            return empleados;
        }
    }
}
