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
    // CAMBIO: restringir la modificación de rutas y tarifas.
    [AutorizacionAdministrativa]
    public class RutasController : Controller
    {
        private AerolineaVIPEntities db = new AerolineaVIPEntities();

        // GET: Rutas
        public ActionResult Index()
        {
            var rutas = db.Rutas.Include(r => r.Aeropuerto).Include(r => r.Aeropuerto1);
            ViewBag.Aviso = TempData["Aviso"] as string;
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
            return View(new Ruta { Activa = true });
        }

        // POST: Rutas/Create
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "RutaId,AeropuertoOrigenId,AeropuertoDestinoId,TarifaReferencial,Activa")] Ruta ruta)
        {
            ValidarRuta(ruta);
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
            var actual = db.Rutas.Find(ruta.RutaId);
            if (actual == null)
            {
                return HttpNotFound();
            }
            var tieneVuelos = db.Vueloes.Any(v => v.Frecuencia.RutaId == actual.RutaId);
            if (tieneVuelos && (actual.AeropuertoOrigenId != ruta.AeropuertoOrigenId || actual.AeropuertoDestinoId != ruta.AeropuertoDestinoId))
            {
                ModelState.AddModelError("", "La ruta ya tiene vuelos. No se pueden cambiar sus aeropuertos; desactive la ruta y cree una nueva para preservar el historial.");
            }
            ValidarRuta(ruta);
            if (ModelState.IsValid)
            {
                actual.TarifaReferencial = ruta.TarifaReferencial;
                actual.Activa = ruta.Activa;
                if (!tieneVuelos)
                {
                    actual.AeropuertoOrigenId = ruta.AeropuertoOrigenId;
                    actual.AeropuertoDestinoId = ruta.AeropuertoDestinoId;
                }
                if (!actual.Activa)
                {
                    foreach (var frecuencia in db.Frecuencias.Where(f => f.RutaId == actual.RutaId))
                    {
                        frecuencia.Activa = false;
                    }
                }
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
            if (ruta == null)
            {
                return HttpNotFound();
            }
            if (db.Frecuencias.Any(f => f.RutaId == id))
            {
                ruta.Activa = false;
                foreach (var frecuencia in db.Frecuencias.Where(f => f.RutaId == id))
                {
                    frecuencia.Activa = false;
                }
                TempData["Aviso"] = "La ruta y sus frecuencias se desactivaron para conservar el historial asociado.";
            }
            else
            {
                db.Rutas.Remove(ruta);
            }
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        private void ValidarRuta(Ruta ruta)
        {
            if (ruta.AeropuertoOrigenId == ruta.AeropuertoDestinoId)
            {
                ModelState.AddModelError("AeropuertoDestinoId", "El aeropuerto de origen y destino deben ser distintos.");
            }
            if (!db.Aeropuertoes.Any(a => a.AeropuertoId == ruta.AeropuertoOrigenId) ||
                !db.Aeropuertoes.Any(a => a.AeropuertoId == ruta.AeropuertoDestinoId))
            {
                ModelState.AddModelError("", "Seleccione aeropuertos existentes para la ruta.");
            }
            else if (ruta.Activa && (!db.Aeropuertoes.Any(a => a.AeropuertoId == ruta.AeropuertoOrigenId && a.Activo) ||
                !db.Aeropuertoes.Any(a => a.AeropuertoId == ruta.AeropuertoDestinoId && a.Activo)))
            {
                ModelState.AddModelError("", "Seleccione aeropuertos activos para la ruta.");
            }
            if (db.Rutas.Any(r => r.AeropuertoOrigenId == ruta.AeropuertoOrigenId &&
                r.AeropuertoDestinoId == ruta.AeropuertoDestinoId && r.RutaId != ruta.RutaId))
            {
                ModelState.AddModelError("AeropuertoDestinoId", "Ya existe una ruta con esos aeropuertos.");
            }
            if (!ModelState.ContainsKey("TarifaReferencial") || ModelState["TarifaReferencial"].Value == null ||
                string.IsNullOrWhiteSpace(ModelState["TarifaReferencial"].Value.AttemptedValue))
            {
                ModelState.AddModelError("TarifaReferencial", "La tarifa es obligatoria.");
            }
            else if (ruta.TarifaReferencial < 0)
            {
                ModelState.AddModelError("TarifaReferencial", "La tarifa no puede ser negativa.");
            }
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
