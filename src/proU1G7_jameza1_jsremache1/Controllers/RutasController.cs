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
    public class RutasController : Controller
    {
        private AerolineaVIPEntities db = new AerolineaVIPEntities();

        // GET: Rutas
        public ActionResult Index()
        {
            var rutas = db.Rutas.Include(r => r.Aeropuerto).Include(r => r.Aeropuerto1);
            return View(rutas.ToList());
        }

        // GET: Rutas/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Ruta ruta = db.Rutas.Find(id);
            if (ruta == null)
            {
                return HttpNotFound();
            }
            return View(ruta);
        }

        // GET: Rutas/Create
        public ActionResult Create()
        {
            ViewBag.AeropuertoDestinoId = new SelectList(db.Aeropuertoes, "AeropuertoId", "CodigoIATA");
            ViewBag.AeropuertoOrigenId = new SelectList(db.Aeropuertoes, "AeropuertoId", "CodigoIATA");
            return View();
        }

        // POST: Rutas/Create
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "RutaId,AeropuertoOrigenId,AeropuertoDestinoId,TarifaReferencial,Activa")] Ruta ruta)
        {
            if (ModelState.IsValid)
            {
                db.Rutas.Add(ruta);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.AeropuertoDestinoId = new SelectList(db.Aeropuertoes, "AeropuertoId", "CodigoIATA", ruta.AeropuertoDestinoId);
            ViewBag.AeropuertoOrigenId = new SelectList(db.Aeropuertoes, "AeropuertoId", "CodigoIATA", ruta.AeropuertoOrigenId);
            return View(ruta);
        }

        // GET: Rutas/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Ruta ruta = db.Rutas.Find(id);
            if (ruta == null)
            {
                return HttpNotFound();
            }
            ViewBag.AeropuertoDestinoId = new SelectList(db.Aeropuertoes, "AeropuertoId", "CodigoIATA", ruta.AeropuertoDestinoId);
            ViewBag.AeropuertoOrigenId = new SelectList(db.Aeropuertoes, "AeropuertoId", "CodigoIATA", ruta.AeropuertoOrigenId);
            return View(ruta);
        }

        // POST: Rutas/Edit/5
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "RutaId,AeropuertoOrigenId,AeropuertoDestinoId,TarifaReferencial,Activa")] Ruta ruta)
        {
            if (ModelState.IsValid)
            {
                db.Entry(ruta).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.AeropuertoDestinoId = new SelectList(db.Aeropuertoes, "AeropuertoId", "CodigoIATA", ruta.AeropuertoDestinoId);
            ViewBag.AeropuertoOrigenId = new SelectList(db.Aeropuertoes, "AeropuertoId", "CodigoIATA", ruta.AeropuertoOrigenId);
            return View(ruta);
        }

        // GET: Rutas/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Ruta ruta = db.Rutas.Find(id);
            if (ruta == null)
            {
                return HttpNotFound();
            }
            return View(ruta);
        }

        // POST: Rutas/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Ruta ruta = db.Rutas.Find(id);
            db.Rutas.Remove(ruta);
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
