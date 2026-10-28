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
    public class FrecuenciaController : Controller
    {
        private AerolineaVIPEntities db = new AerolineaVIPEntities();

        // GET: Frecuencia
        public ActionResult Index()
        {
            var frecuencias = db.Frecuencias.Include(f => f.Ruta);
            return View(frecuencias.ToList());
        }

        // GET: Frecuencia/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Frecuencia frecuencia = db.Frecuencias.Find(id);
            if (frecuencia == null)
            {
                return HttpNotFound();
            }
            return View(frecuencia);
        }

        // GET: Frecuencia/Create
        public ActionResult Create()
        {
            ViewBag.RutaId = new SelectList(db.Rutas, "RutaId", "RutaId");
            return View();
        }

        // POST: Frecuencia/Create
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "FrecuenciaId,RutaId,DiaSemana,HoraSalida,HoraLlegada,Activa")] Frecuencia frecuencia)
        {
            if (ModelState.IsValid)
            {
                db.Frecuencias.Add(frecuencia);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.RutaId = new SelectList(db.Rutas, "RutaId", "RutaId", frecuencia.RutaId);
            return View(frecuencia);
        }

        // GET: Frecuencia/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Frecuencia frecuencia = db.Frecuencias.Find(id);
            if (frecuencia == null)
            {
                return HttpNotFound();
            }
            ViewBag.RutaId = new SelectList(db.Rutas, "RutaId", "RutaId", frecuencia.RutaId);
            return View(frecuencia);
        }

        // POST: Frecuencia/Edit/5
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "FrecuenciaId,RutaId,DiaSemana,HoraSalida,HoraLlegada,Activa")] Frecuencia frecuencia)
        {
            if (ModelState.IsValid)
            {
                db.Entry(frecuencia).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.RutaId = new SelectList(db.Rutas, "RutaId", "RutaId", frecuencia.RutaId);
            return View(frecuencia);
        }

        // GET: Frecuencia/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Frecuencia frecuencia = db.Frecuencias.Find(id);
            if (frecuencia == null)
            {
                return HttpNotFound();
            }
            return View(frecuencia);
        }

        // POST: Frecuencia/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Frecuencia frecuencia = db.Frecuencias.Find(id);
            db.Frecuencias.Remove(frecuencia);
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
