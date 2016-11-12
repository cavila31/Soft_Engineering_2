using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Activos_PrestamosOET.Models;

namespace Activos_PrestamosOET.Controllers
{
    public class TransaccionesController : Controller
    {

        private PrestamosEntities db = new PrestamosEntities();

        public bool Create(string responsable, string estado, string descripcion, string activo_id)
        {
            TRANSACCION nueva_transaccion = new TRANSACCION();
            nueva_transaccion.FECHA = DateTime.Now;
            nueva_transaccion.ESTADO = estado;
            nueva_transaccion.DESCRIPCION = descripcion;
            nueva_transaccion.ACTIVOID = activo_id;
            nueva_transaccion.RESPONSABLE = responsable;

            if (ModelState.IsValid)
            {
                db.TRANSACCIONES.Add(nueva_transaccion);
                db.SaveChanges();
                return true;
            }

            return false;
        }

        public bool CreateWithResponsible(string responsable, string estado, string descripcion, string activo_id, string responsable_de_activo)
        {
            TRANSACCION nueva_transaccion = new TRANSACCION();
            nueva_transaccion.FECHA = DateTime.Now;
            nueva_transaccion.ESTADO = estado;
            nueva_transaccion.DESCRIPCION = descripcion;
            nueva_transaccion.ACTIVOID = activo_id;
            nueva_transaccion.RESPONSABLE = responsable;
            nueva_transaccion.V_EMPLEADOSIDEMPLEADO = responsable_de_activo;

            if (ModelState.IsValid)
            {
                db.TRANSACCIONES.Add(nueva_transaccion);
                db.SaveChanges();
                return true;
            }

            return false;
        }

        public bool CreatePrestamo(string responsable, string estado, string descripcion, string activo_id, int numBoleta, DateTime retiro, DateTime devolucion, string observ, string solicitante)
        {
            TRANSACCION nueva_transaccion = new TRANSACCION();
            nueva_transaccion.FECHA = DateTime.Now;
            nueva_transaccion.ESTADO = estado;
            nueva_transaccion.DESCRIPCION = descripcion;
            nueva_transaccion.ACTIVOID = activo_id;
            nueva_transaccion.RESPONSABLE = responsable;
            nueva_transaccion.NUMERO_BOLETA = numBoleta;
            nueva_transaccion.FECHA_RETIRO = retiro;
            nueva_transaccion.FECHA_DEVOLUCION = devolucion;
            nueva_transaccion.OBSERVACIONES_RECIBO = observ;
            nueva_transaccion.NOMBRE_SOLICITANTE = solicitante;

            if (ModelState.IsValid)
            {
                db.TRANSACCIONES.Add(nueva_transaccion);
                db.SaveChanges();
                return true;
            }

            return false;
        }

    }
}