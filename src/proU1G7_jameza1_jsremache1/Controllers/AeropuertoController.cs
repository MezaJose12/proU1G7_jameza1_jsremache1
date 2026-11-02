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
    // CAMBIO: proteger el CRUD de catálogos; el acceso público es solo de consulta de vuelos.
    [Authorize(Roles = "Administrador,SuperUsuario")]
    public class AeropuertoController : Controller
    {
        private AerolineaVIPEntities db = new AerolineaVIPEntities();

        // GET: Aeropuerto
        public ActionResult Index()
        {
            return View(db.Aeropuertoes.ToList());
        }

        // GET: Aeropuerto/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Aeropuerto aeropuerto = db.Aeropuertoes.Find(id);
            if (aeropuerto == null)
            {
                return HttpNotFound();
            }
            return View(aeropuerto);
        }

        // GET: Aeropuerto/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Aeropuerto/Create
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "AeropuertoId,CodigoIATA,Nombre,Localidad,Activo")] Aeropuerto aeropuerto)
        {
            if (ModelState.IsValid)
            {
                db.Aeropuertoes.Add(aeropuerto);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(aeropuerto);
        }

        // GET: Aeropuerto/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Aeropuerto aeropuerto = db.Aeropuertoes.Find(id);
            if (aeropuerto == null)
            {
                return HttpNotFound();
            }
            return View(aeropuerto);
        }

        // POST: Aeropuerto/Edit/5
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "AeropuertoId,CodigoIATA,Nombre,Localidad,Activo")] Aeropuerto aeropuerto)
        {
            var actual = db.Aeropuertoes.Find(aeropuerto.AeropuertoId);
            if (actual == null)
            {
                return HttpNotFound();
            }

            var cambiaDatosHistoricos = !string.Equals(actual.CodigoIATA, aeropuerto.CodigoIATA, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(actual.Nombre, aeropuerto.Nombre, StringComparison.Ordinal) ||
                !string.Equals(actual.Localidad, aeropuerto.Localidad, StringComparison.Ordinal);
            var usadoPorVueloCompletado = db.Vueloes.Any(v => v.EstadoVueloCodigo == "COMPLETADO" &&
                (v.Frecuencia.Ruta.AeropuertoOrigenId == aeropuerto.AeropuertoId ||
                 v.Frecuencia.Ruta.AeropuertoDestinoId == aeropuerto.AeropuertoId));
            if (cambiaDatosHistoricos && usadoPorVueloCompletado)
            {
                ModelState.AddModelError("", "DP-01: no se pueden cambiar los datos de un aeropuerto asociado a vuelos completados.");
            }

            if (ModelState.IsValid)
            {
                db.Entry(actual).CurrentValues.SetValues(aeropuerto);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(aeropuerto);
        }

        // GET: Aeropuerto/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Aeropuerto aeropuerto = db.Aeropuertoes.Find(id);
            if (aeropuerto == null)
            {
                return HttpNotFound();
            }
            return View(aeropuerto);
        }

        // POST: Aeropuerto/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Aeropuerto aeropuerto = db.Aeropuertoes.Find(id);
            db.Aeropuertoes.Remove(aeropuerto);
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
