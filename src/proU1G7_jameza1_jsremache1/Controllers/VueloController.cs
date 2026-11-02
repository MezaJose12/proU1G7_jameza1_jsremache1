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
    // CAMBIO: restringir la creación y edición del inventario de vuelos.
    [Authorize(Roles = "Administrador,SuperUsuario")]
    public class VueloController : Controller
    {
        private AerolineaVIPEntities db = new AerolineaVIPEntities();

        // GET: Vuelo
        public ActionResult Index()
        {
            var vueloes = db.Vueloes.Include(v => v.Aeronave).Include(v => v.EstadoVuelo).Include(v => v.Frecuencia);
            return View(vueloes.ToList());
        }

        // GET: Vuelo/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Vuelo vuelo = db.Vueloes.Find(id);
            if (vuelo == null)
            {
                return HttpNotFound();
            }
            return View(vuelo);
        }

        // GET: Vuelo/Create
        public ActionResult Create()
        {
            ViewBag.AeronaveId = new SelectList(db.Aeronaves, "AeronaveId", "Matricula");
            ViewBag.EstadoVueloCodigo = new SelectList(db.EstadoVueloes, "Codigo", "Descripcion");
            ViewBag.FrecuenciaId = new SelectList(db.Frecuencias, "FrecuenciaId", "FrecuenciaId");
            return View();
        }

        // POST: Vuelo/Create
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "VueloId,FrecuenciaId,FechaOperacion,AeronaveId,EstadoVueloCodigo")] Vuelo vuelo)
        {
            if (vuelo.AeronaveId == null && vuelo.EstadoVueloCodigo != "PROGRAMADO")
            {
                ModelState.AddModelError("EstadoVueloCodigo", "DP-02: un vuelo sin aeronave debe permanecer PROGRAMADO.");
            }
            if (vuelo.AeronaveId.HasValue && !db.Aeronaves.Any(a => a.AeronaveId == vuelo.AeronaveId && a.EstadoAeronaveCodigo == "DISPONIBLE"))
            {
                ModelState.AddModelError("AeronaveId", "Seleccione una aeronave disponible.");
            }

            if (ModelState.IsValid)
            {
                db.Vueloes.Add(vuelo);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.AeronaveId = new SelectList(db.Aeronaves, "AeronaveId", "Matricula", vuelo.AeronaveId);
            ViewBag.EstadoVueloCodigo = new SelectList(db.EstadoVueloes, "Codigo", "Descripcion", vuelo.EstadoVueloCodigo);
            ViewBag.FrecuenciaId = new SelectList(db.Frecuencias, "FrecuenciaId", "FrecuenciaId", vuelo.FrecuenciaId);
            return View(vuelo);
        }

        // GET: Vuelo/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Vuelo vuelo = db.Vueloes.Find(id);
            if (vuelo == null)
            {
                return HttpNotFound();
            }
            ViewBag.AeronaveId = new SelectList(db.Aeronaves, "AeronaveId", "Matricula", vuelo.AeronaveId);
            ViewBag.EstadoVueloCodigo = new SelectList(db.EstadoVueloes, "Codigo", "Descripcion", vuelo.EstadoVueloCodigo);
            ViewBag.FrecuenciaId = new SelectList(db.Frecuencias, "FrecuenciaId", "FrecuenciaId", vuelo.FrecuenciaId);
            return View(vuelo);
        }

        // POST: Vuelo/Edit/5
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "VueloId,FrecuenciaId,FechaOperacion,AeronaveId,EstadoVueloCodigo")] Vuelo vuelo)
        {
            var actual = db.Vueloes.Find(vuelo.VueloId);
            if (actual == null)
            {
                return HttpNotFound();
            }
            if (actual.EstadoVueloCodigo == "COMPLETADO" && actual.AeronaveId != vuelo.AeronaveId)
            {
                ModelState.AddModelError("AeronaveId", "DP-02: no se puede cambiar la aeronave de un vuelo completado.");
            }
            if (vuelo.AeronaveId == null && vuelo.EstadoVueloCodigo != "PROGRAMADO")
            {
                ModelState.AddModelError("EstadoVueloCodigo", "DP-02: un vuelo sin aeronave debe permanecer PROGRAMADO.");
            }
            if (vuelo.AeronaveId.HasValue && !db.Aeronaves.Any(a => a.AeronaveId == vuelo.AeronaveId && a.EstadoAeronaveCodigo == "DISPONIBLE"))
            {
                ModelState.AddModelError("AeronaveId", "Seleccione una aeronave disponible.");
            }

            if (ModelState.IsValid)
            {
                actual.FrecuenciaId = vuelo.FrecuenciaId;
                actual.FechaOperacion = vuelo.FechaOperacion;
                actual.AeronaveId = vuelo.AeronaveId;
                actual.EstadoVueloCodigo = vuelo.EstadoVueloCodigo;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.AeronaveId = new SelectList(db.Aeronaves, "AeronaveId", "Matricula", vuelo.AeronaveId);
            ViewBag.EstadoVueloCodigo = new SelectList(db.EstadoVueloes, "Codigo", "Descripcion", vuelo.EstadoVueloCodigo);
            ViewBag.FrecuenciaId = new SelectList(db.Frecuencias, "FrecuenciaId", "FrecuenciaId", vuelo.FrecuenciaId);
            return View(vuelo);
        }

        // GET: Vuelo/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Vuelo vuelo = db.Vueloes.Find(id);
            if (vuelo == null)
            {
                return HttpNotFound();
            }
            return View(vuelo);
        }

        // POST: Vuelo/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Vuelo vuelo = db.Vueloes.Find(id);
            db.Vueloes.Remove(vuelo);
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
