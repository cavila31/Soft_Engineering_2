using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Activos_PrestamosOET.Models;

namespace Activos_PrestamosOET.Controllers
{
    [Authorize(Roles = "superadmin")]
    public class TiposTransaccionesController : Controller
    {
        private PrestamosEntities db = new PrestamosEntities();

        // GET: TiposTransacciones
        public ActionResult Index()
        {
            return View(db.TIPOS_TRANSACCIONES.ToList());
        }

        // GET: TiposTransacciones/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            TIPOS_TRANSACCIONES tIPOS_TRANSACCIONES = db.TIPOS_TRANSACCIONES.Find(id);
            if (tIPOS_TRANSACCIONES == null)
            {
                return HttpNotFound();
            }
            return View(tIPOS_TRANSACCIONES);
        }

        // GET: TiposTransacciones/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: TiposTransacciones/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "ID,NOMBRE")] TIPOS_TRANSACCIONES tIPOS_TRANSACCIONES)
        {
            if (ModelState.IsValid)
            {
                db.TIPOS_TRANSACCIONES.Add(tIPOS_TRANSACCIONES);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(tIPOS_TRANSACCIONES);
        }

        // GET: TiposTransacciones/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            TIPOS_TRANSACCIONES tIPOS_TRANSACCIONES = db.TIPOS_TRANSACCIONES.Find(id);
            if (tIPOS_TRANSACCIONES == null)
            {
                return HttpNotFound();
            }
            return View(tIPOS_TRANSACCIONES);
        }

        // POST: TiposTransacciones/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "ID,NOMBRE")] TIPOS_TRANSACCIONES tIPOS_TRANSACCIONES)
        {
            if (ModelState.IsValid)
            {
                db.Entry(tIPOS_TRANSACCIONES).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(tIPOS_TRANSACCIONES);
        }

        // GET: TiposTransacciones/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            TIPOS_TRANSACCIONES tIPOS_TRANSACCIONES = db.TIPOS_TRANSACCIONES.Find(id);
            if (tIPOS_TRANSACCIONES == null)
            {
                return HttpNotFound();
            }
            return View(tIPOS_TRANSACCIONES);
        }

        // POST: TiposTransacciones/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            TIPOS_TRANSACCIONES tIPOS_TRANSACCIONES = db.TIPOS_TRANSACCIONES.Find(id);
            db.TIPOS_TRANSACCIONES.Remove(tIPOS_TRANSACCIONES);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
