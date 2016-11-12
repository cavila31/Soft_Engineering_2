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
    public class EQUIPO_SOLICITADOController : Controller
    {
        private PrestamosEntities db = new PrestamosEntities();

        // GET: EQUIPO_SOLICITADO
        public ActionResult Index()
        {
            var eQUIPO_SOLICITADO = db.EQUIPO_SOLICITADO.Include(e => e.PRESTAMO);
            return View(eQUIPO_SOLICITADO.ToList());
        }

        // GET: EQUIPO_SOLICITADO/Details/5
        public ActionResult Details(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            EQUIPO_SOLICITADO eQUIPO_SOLICITADO = db.EQUIPO_SOLICITADO.Find(id);
            if (eQUIPO_SOLICITADO == null)
            {
                return HttpNotFound();
            }
            return View(eQUIPO_SOLICITADO);
        }

        // GET: EQUIPO_SOLICITADO/Create
        public ActionResult Create()
        {
            ViewBag.ID_PRESTAMO = new SelectList(db.PRESTAMOS, "ID", "MOTIVO");
            return View();
        }

        // POST: EQUIPO_SOLICITADO/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "ID_PRESTAMO,TIPO_ACTIVO,CANTIDAD")] EQUIPO_SOLICITADO eQUIPO_SOLICITADO)
        {
            if (ModelState.IsValid)
            {
                db.EQUIPO_SOLICITADO.Add(eQUIPO_SOLICITADO);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.ID_PRESTAMO = new SelectList(db.PRESTAMOS, "ID", "MOTIVO", eQUIPO_SOLICITADO.ID_PRESTAMO);
            return View(eQUIPO_SOLICITADO);
        }

        // GET: EQUIPO_SOLICITADO/Edit/5
        public ActionResult Edit(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            EQUIPO_SOLICITADO eQUIPO_SOLICITADO = db.EQUIPO_SOLICITADO.Find(id);
            if (eQUIPO_SOLICITADO == null)
            {
                return HttpNotFound();
            }
            ViewBag.ID_PRESTAMO = new SelectList(db.PRESTAMOS, "ID", "MOTIVO", eQUIPO_SOLICITADO.ID_PRESTAMO);
            return View(eQUIPO_SOLICITADO);
        }

        // POST: EQUIPO_SOLICITADO/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "ID_PRESTAMO,TIPO_ACTIVO,CANTIDAD")] EQUIPO_SOLICITADO eQUIPO_SOLICITADO)
        {
            if (ModelState.IsValid)
            {
                db.Entry(eQUIPO_SOLICITADO).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.ID_PRESTAMO = new SelectList(db.PRESTAMOS, "ID", "MOTIVO", eQUIPO_SOLICITADO.ID_PRESTAMO);
            return View(eQUIPO_SOLICITADO);
        }

        // GET: EQUIPO_SOLICITADO/Delete/5
        public ActionResult Delete(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            EQUIPO_SOLICITADO eQUIPO_SOLICITADO = db.EQUIPO_SOLICITADO.Find(id);
            if (eQUIPO_SOLICITADO == null)
            {
                return HttpNotFound();
            }
            return View(eQUIPO_SOLICITADO);
        }

        // POST: EQUIPO_SOLICITADO/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(string id)
        {
            EQUIPO_SOLICITADO eQUIPO_SOLICITADO = db.EQUIPO_SOLICITADO.Find(id);
            db.EQUIPO_SOLICITADO.Remove(eQUIPO_SOLICITADO);
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
