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
    public class EstadosActivosController : Controller
    {
        private PrestamosEntities db = new PrestamosEntities();

        // GET: EstadosActivos
        public ActionResult Index()
        {
            return View(db.ESTADOS_ACTIVOS.ToList());
        }

        // GET: EstadosActivos/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ESTADOS_ACTIVOS eSTADOS_ACTIVOS = db.ESTADOS_ACTIVOS.Find(id);
            if (eSTADOS_ACTIVOS == null)
            {
                return HttpNotFound();
            }
            return View(eSTADOS_ACTIVOS);
        }

        // GET: EstadosActivos/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: EstadosActivos/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "ID,NOMBRE")] ESTADOS_ACTIVOS eSTADOS_ACTIVOS)
        {
            if (ModelState.IsValid)
            {
                db.ESTADOS_ACTIVOS.Add(eSTADOS_ACTIVOS);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(eSTADOS_ACTIVOS);
        }

        // GET: EstadosActivos/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ESTADOS_ACTIVOS eSTADOS_ACTIVOS = db.ESTADOS_ACTIVOS.Find(id);
            if (eSTADOS_ACTIVOS == null)
            {
                return HttpNotFound();
            }
            return View(eSTADOS_ACTIVOS);
        }

        // POST: EstadosActivos/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "ID,NOMBRE")] ESTADOS_ACTIVOS eSTADOS_ACTIVOS)
        {
            if (ModelState.IsValid)
            {
                db.Entry(eSTADOS_ACTIVOS).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(eSTADOS_ACTIVOS);
        }

        // GET: EstadosActivos/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ESTADOS_ACTIVOS eSTADOS_ACTIVOS = db.ESTADOS_ACTIVOS.Find(id);
            if (eSTADOS_ACTIVOS == null)
            {
                return HttpNotFound();
            }
            return View(eSTADOS_ACTIVOS);
        }

        // POST: EstadosActivos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            ESTADOS_ACTIVOS eSTADOS_ACTIVOS = db.ESTADOS_ACTIVOS.Find(id);
            db.ESTADOS_ACTIVOS.Remove(eSTADOS_ACTIVOS);
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
