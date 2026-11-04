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
    [AutorizacionAdministrativa]
    public class AeropuertoController : Controller
    {
        private AerolineaVIPEntities db = new AerolineaVIPEntities();

        // GET: Aeropuerto
        public ActionResult Index()
        {
            ViewBag.Aviso = TempData["Aviso"] as string;
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
            return View(new Aeropuerto { Activo = true });
        }

        // POST: Aeropuerto/Create
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "AeropuertoId,CodigoIATA,Nombre,Localidad,Activo")] Aeropuerto aeropuerto)
        {
            ValidarAeropuerto(aeropuerto);
            if (ModelState.IsValid)
            {
                NormalizarAeropuerto(aeropuerto);
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

            ValidarAeropuerto(aeropuerto);
            if (ModelState.IsValid)
            {
                NormalizarAeropuerto(aeropuerto);
                actual.CodigoIATA = aeropuerto.CodigoIATA;
                actual.Nombre = aeropuerto.Nombre;
                actual.Localidad = aeropuerto.Localidad;
                actual.Activo = aeropuerto.Activo;
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
            if (aeropuerto == null)
            {
                return HttpNotFound();
            }
            if (db.Rutas.Any(r => r.AeropuertoOrigenId == id || r.AeropuertoDestinoId == id))
            {
                aeropuerto.Activo = false;
                TempData["Aviso"] = "El aeropuerto se desactivo porque esta asociado a rutas; su historial se conserva.";
            }
            else
            {
                db.Aeropuertoes.Remove(aeropuerto);
            }
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        private void ValidarAeropuerto(Aeropuerto aeropuerto)
        {
            var codigo = aeropuerto.CodigoIATA == null ? string.Empty : aeropuerto.CodigoIATA.Trim().ToUpperInvariant();
            var nombre = aeropuerto.Nombre == null ? string.Empty : aeropuerto.Nombre.Trim();
            var localidad = aeropuerto.Localidad == null ? string.Empty : aeropuerto.Localidad.Trim();
            if (codigo.Length != 3 || codigo.Any(c => c < 'A' || c > 'Z'))
            {
                ModelState.AddModelError("CodigoIATA", "El codigo IATA debe tener tres letras mayusculas.");
            }
            else if (db.Aeropuertoes.Any(a => a.CodigoIATA == codigo && a.AeropuertoId != aeropuerto.AeropuertoId))
            {
                ModelState.AddModelError("CodigoIATA", "Ya existe un aeropuerto con ese codigo IATA.");
            }
            if (string.IsNullOrWhiteSpace(nombre) || nombre.Length > 150)
            {
                ModelState.AddModelError("Nombre", "El nombre es obligatorio y no debe superar 150 caracteres.");
            }
            if (string.IsNullOrWhiteSpace(localidad) || localidad.Length > 100)
            {
                ModelState.AddModelError("Localidad", "La localidad es obligatoria y no debe superar 100 caracteres.");
            }
        }

        private static void NormalizarAeropuerto(Aeropuerto aeropuerto)
        {
            aeropuerto.CodigoIATA = aeropuerto.CodigoIATA.Trim().ToUpperInvariant();
            aeropuerto.Nombre = aeropuerto.Nombre.Trim();
            aeropuerto.Localidad = aeropuerto.Localidad.Trim();
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
