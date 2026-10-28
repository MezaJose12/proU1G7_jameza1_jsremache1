using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using proU1G7_jameza1_jsremache1.Models;

namespace proU1G7_jameza1_jsremache1.Controllers
{
    public class AsientoController : Controller
    {
        private AerolineaVIPEntities db = new AerolineaVIPEntities();

        // GET: Asiento
        public ActionResult Index()
        {
            var asientoes = db.Asientoes.Include(a => a.ModeloAeronave);
            return View(asientoes.ToList());
        }

        // GET: Asiento/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Asiento asiento = db.Asientoes.Find(id);
            if (asiento == null)
            {
                return HttpNotFound();
            }
            return View(asiento);
        }

        // GET: Asiento/Create
        public ActionResult Create()
        {
            ViewBag.ModeloAeronaveId = new SelectList(db.ModeloAeronaves, "ModeloAeronaveId", "Fabricante");
            return View();
        }

        // POST: Asiento/Create
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "ModeloAeronaveId,NumeroAsiento,Codigo,Fila,Lado,Ubicacion")] Asiento asiento)
        {
            if (ModelState.IsValid)
            {
                db.Asientoes.Add(asiento);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.ModeloAeronaveId = new SelectList(db.ModeloAeronaves, "ModeloAeronaveId", "Fabricante", asiento.ModeloAeronaveId);
            return View(asiento);
        }

        // GET: Asiento/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Asiento asiento = db.Asientoes.Find(id);
            if (asiento == null)
            {
                return HttpNotFound();
            }
            ViewBag.ModeloAeronaveId = new SelectList(db.ModeloAeronaves, "ModeloAeronaveId", "Fabricante", asiento.ModeloAeronaveId);
            return View(asiento);
        }

        // POST: Asiento/Edit/5
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "ModeloAeronaveId,NumeroAsiento,Codigo,Fila,Lado,Ubicacion")] Asiento asiento)
        {
            if (ModelState.IsValid)
            {
                db.Entry(asiento).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.ModeloAeronaveId = new SelectList(db.ModeloAeronaves, "ModeloAeronaveId", "Fabricante", asiento.ModeloAeronaveId);
            return View(asiento);
        }

        // GET: Asiento/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Asiento asiento = db.Asientoes.Find(id);
            if (asiento == null)
            {
                return HttpNotFound();
            }
            return View(asiento);
        }

        // POST: Asiento/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Asiento asiento = db.Asientoes.Find(id);
            db.Asientoes.Remove(asiento);
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
