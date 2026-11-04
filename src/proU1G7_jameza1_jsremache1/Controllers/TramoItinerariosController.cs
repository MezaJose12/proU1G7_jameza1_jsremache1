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
    // CAMBIO: restringir la edición de los tramos asignados a cada itinerario.
    [AutorizacionAdministrativa]
    public class TramoItinerariosController : Controller
    {
        private AerolineaVIPEntities db = new AerolineaVIPEntities();

        // GET: TramoItinerarios
        public ActionResult Index()
        {
            var tramoItinerarios = db.TramoItinerarios
                .Include(t => t.Itinerario)
                .Include(t => t.Vuelo.Frecuencia.Ruta.Aeropuerto)
                .Include(t => t.Vuelo.Frecuencia.Ruta.Aeropuerto1)
                .OrderBy(t => t.ItinerarioId)
                .ThenBy(t => t.Orden);
            return View(tramoItinerarios.ToList());
        }

        // GET: TramoItinerarios/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            TramoItinerario tramoItinerario = db.TramoItinerarios.Find(id);
            if (tramoItinerario == null)
            {
                return HttpNotFound();
            }
            return View(tramoItinerario);
        }

        // GET: TramoItinerarios/Create
        public ActionResult Create(int? itinerarioId)
        {
            ViewBag.ItinerarioId = CrearOpcionesItinerario(itinerarioId);
            ViewBag.VueloId = CrearOpcionesVuelo();
            return View(new TramoItinerario { ItinerarioId = itinerarioId.GetValueOrDefault() });
        }

        // POST: TramoItinerarios/Create
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "TramoItinerarioId,ItinerarioId,VueloId,Orden")] TramoItinerario tramoItinerario)
        {
            if (ModelState.IsValid)
            {
                db.TramoItinerarios.Add(tramoItinerario);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.ItinerarioId = CrearOpcionesItinerario(tramoItinerario.ItinerarioId);
            ViewBag.VueloId = CrearOpcionesVuelo(tramoItinerario.VueloId);
            return View(tramoItinerario);
        }

        // GET: TramoItinerarios/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            TramoItinerario tramoItinerario = db.TramoItinerarios.Find(id);
            if (tramoItinerario == null)
            {
                return HttpNotFound();
            }
            ViewBag.ItinerarioId = CrearOpcionesItinerario(tramoItinerario.ItinerarioId);
            ViewBag.VueloId = CrearOpcionesVuelo(tramoItinerario.VueloId);
            return View(tramoItinerario);
        }

        // POST: TramoItinerarios/Edit/5
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "TramoItinerarioId,ItinerarioId,VueloId,Orden")] TramoItinerario tramoItinerario)
        {
            if (ModelState.IsValid)
            {
                db.Entry(tramoItinerario).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.ItinerarioId = CrearOpcionesItinerario(tramoItinerario.ItinerarioId);
            ViewBag.VueloId = CrearOpcionesVuelo(tramoItinerario.VueloId);
            return View(tramoItinerario);
        }

        // GET: TramoItinerarios/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            TramoItinerario tramoItinerario = db.TramoItinerarios.Find(id);
            if (tramoItinerario == null)
            {
                return HttpNotFound();
            }
            return View(tramoItinerario);
        }

        // POST: TramoItinerarios/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            TramoItinerario tramoItinerario = db.TramoItinerarios.Find(id);
            db.TramoItinerarios.Remove(tramoItinerario);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        private SelectList CrearOpcionesItinerario(int? seleccionado = null)
        {
            var itinerarios = db.Itinerarios
                .OrderBy(i => i.Referencia)
                .ToList()
                .Select(i => new { i.ItinerarioId, Descripcion = i.Referencia + " - " + i.Nombre })
                .ToList();
            return new SelectList(itinerarios, "ItinerarioId", "Descripcion", seleccionado);
        }

        private SelectList CrearOpcionesVuelo(int? seleccionado = null)
        {
            var vuelos = db.Vueloes
                .Include(v => v.Frecuencia.Ruta.Aeropuerto)
                .Include(v => v.Frecuencia.Ruta.Aeropuerto1)
                .OrderBy(v => v.FechaOperacion)
                .ToList()
                .Select(v => new
                {
                    v.VueloId,
                    Descripcion = "Vuelo " + v.VueloId + " | " +
                        v.Frecuencia.Ruta.Aeropuerto.CodigoIATA + " -> " + v.Frecuencia.Ruta.Aeropuerto1.CodigoIATA +
                        " | " + v.FechaOperacion.ToString("dd/MM/yyyy") + " " +
                        v.Frecuencia.HoraSalida.ToString(@"hh\:mm") + "-" + v.Frecuencia.HoraLlegada.ToString(@"hh\:mm")
                })
                .ToList();
            return new SelectList(vuelos, "VueloId", "Descripcion", seleccionado);
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
