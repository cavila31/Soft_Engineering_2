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
    public class TiposActivosController : Controller
    {
        private PrestamosEntities db = new PrestamosEntities();

        // GET: TiposActivos
        public ActionResult Index()
        {
            return View(db.TIPOS_ACTIVOS.ToList());
        }

        // GET: TiposActivos/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            TIPOS_ACTIVOS tIPOS_ACTIVOS = db.TIPOS_ACTIVOS.Find(id);
            if (tIPOS_ACTIVOS == null)
            {
                return HttpNotFound();
            }
            return View(tIPOS_ACTIVOS);
        }

        // GET: TiposActivos/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: TiposActivos/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "ID,NOMBRE")] TIPOS_ACTIVOS tIPOS_ACTIVOS)
        {
            if (ModelState.IsValid)
            {
                db.TIPOS_ACTIVOS.Add(tIPOS_ACTIVOS);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(tIPOS_ACTIVOS);
        }

        // GET: TiposActivos/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            TIPOS_ACTIVOS tIPOS_ACTIVOS = db.TIPOS_ACTIVOS.Find(id);
            if (tIPOS_ACTIVOS == null)
            {
                return HttpNotFound();
            }
            return View(tIPOS_ACTIVOS);
        }

        // POST: TiposActivos/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "ID,NOMBRE")] TIPOS_ACTIVOS tIPOS_ACTIVOS)
        {
            if (ModelState.IsValid)
            {
                db.Entry(tIPOS_ACTIVOS).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(tIPOS_ACTIVOS);
        }

        // GET: TiposActivos/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            TIPOS_ACTIVOS tIPOS_ACTIVOS = db.TIPOS_ACTIVOS.Find(id);
            if (tIPOS_ACTIVOS == null)
            {
                return HttpNotFound();
            }
            return View(tIPOS_ACTIVOS);
        }

        // POST: TiposActivos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            TIPOS_ACTIVOS tIPOS_ACTIVOS = db.TIPOS_ACTIVOS.Find(id);
            db.TIPOS_ACTIVOS.Remove(tIPOS_ACTIVOS);
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
