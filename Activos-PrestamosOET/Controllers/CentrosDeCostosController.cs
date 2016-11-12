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
    public class CentrosDeCostosController : Controller
    {
        private PrestamosEntities db = new PrestamosEntities();

        // GET: CentrosDeCostos
        public ActionResult Index()
        {
            return View(db.CENTROS_DE_COSTOS.ToList());
        }

        // GET: CentrosDeCostos/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            CENTROS_DE_COSTOS cENTROS_DE_COSTOS = db.CENTROS_DE_COSTOS.Find(id);
            if (cENTROS_DE_COSTOS == null)
            {
                return HttpNotFound();
            }
            return View(cENTROS_DE_COSTOS);
        }

        // GET: CentrosDeCostos/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: CentrosDeCostos/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,Nombre")] CENTROS_DE_COSTOS cENTROS_DE_COSTOS)
        {
            if (ModelState.IsValid)
            {
                db.CENTROS_DE_COSTOS.Add(cENTROS_DE_COSTOS);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(cENTROS_DE_COSTOS);
        }

        // GET: CentrosDeCostos/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            CENTROS_DE_COSTOS cENTROS_DE_COSTOS = db.CENTROS_DE_COSTOS.Find(id);
            if (cENTROS_DE_COSTOS == null)
            {
                return HttpNotFound();
            }
            return View(cENTROS_DE_COSTOS);
        }

        // POST: CentrosDeCostos/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,Nombre")] CENTROS_DE_COSTOS cENTROS_DE_COSTOS)
        {
            if (ModelState.IsValid)
            {
                db.Entry(cENTROS_DE_COSTOS).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(cENTROS_DE_COSTOS);
        }

        // GET: CentrosDeCostos/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            CENTROS_DE_COSTOS cENTROS_DE_COSTOS = db.CENTROS_DE_COSTOS.Find(id);
            if (cENTROS_DE_COSTOS == null)
            {
                return HttpNotFound();
            }
            return View(cENTROS_DE_COSTOS);
        }

        // POST: CentrosDeCostos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            CENTROS_DE_COSTOS cENTROS_DE_COSTOS = db.CENTROS_DE_COSTOS.Find(id);
            db.CENTROS_DE_COSTOS.Remove(cENTROS_DE_COSTOS);
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
