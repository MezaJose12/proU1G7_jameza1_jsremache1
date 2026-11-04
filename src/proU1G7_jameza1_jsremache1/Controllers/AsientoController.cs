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
    // CAMBIO: proteger el CRUD de la configuración física de asientos.
    [AutorizacionAdministrativa]
    public class AsientoController : Controller
    {
        private AerolineaVIPEntities db = new AerolineaVIPEntities();

        // GET: Asiento
        public ActionResult Index()
        {
            var modelos = db.ModeloAeronaves
                .Include(m => m.Asientoes)
                .Include(m => m.Aeronaves)
                .OrderBy(m => m.Fabricante)
                .ThenBy(m => m.Nombre);
            ViewBag.Aviso = TempData["Aviso"] as string;
            return View(modelos.ToList());
        }

        // GET: Asiento/Details/5
        public ActionResult Details(int? modeloAeronaveId, byte? numeroAsiento)
        {
            if (!modeloAeronaveId.HasValue || !numeroAsiento.HasValue)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Asiento asiento = db.Asientoes.Find(modeloAeronaveId.Value, numeroAsiento.Value);
            if (asiento == null)
            {
                return HttpNotFound();
            }
            return View(asiento);
        }

        // GET: Asiento/Create
        public ActionResult Create(int? modeloAeronaveId, byte? numeroAsiento, byte? fila, string lado)
        {
            PrepararOpcionesModelo(modeloAeronaveId);
            var asiento = new Asiento { ModeloAeronaveId = modeloAeronaveId.GetValueOrDefault() };
            if (numeroAsiento.HasValue && fila.HasValue && (lado == "D" || lado == "I"))
            {
                asiento.NumeroAsiento = numeroAsiento.Value;
                asiento.Fila = fila.Value;
                asiento.Lado = lado;
                asiento.Codigo = fila.Value + lado;
            }

            return View(asiento);
        }

        // POST: Asiento/Create
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "ModeloAeronaveId,NumeroAsiento,Codigo,Fila,Lado,Ubicacion")] Asiento asiento)
        {
            NormalizarAsiento(asiento);
            ValidarAsiento(asiento, null);
            if (ModelState.IsValid)
            {
                db.Asientoes.Add(asiento);
                db.SaveChanges();
                var modelo = db.ModeloAeronaves.Find(asiento.ModeloAeronaveId);
                var totalAsientos = db.Asientoes.Count(a => a.ModeloAeronaveId == asiento.ModeloAeronaveId);
                if (totalAsientos == modelo.CapacidadAsientos)
                {
                    TempData["Aviso"] = "La distribucion quedo completa. Ya puede registrar una aeronave de este modelo.";
                    return RedirectToAction("Index");
                }
                return RedirectToAction("Create", new { modeloAeronaveId = asiento.ModeloAeronaveId });
            }

            PrepararOpcionesModelo(asiento.ModeloAeronaveId);
            return View(asiento);
        }

        // GET: Asiento/Edit/5
        public ActionResult Edit(int? modeloAeronaveId, byte? numeroAsiento)
        {
            if (!modeloAeronaveId.HasValue || !numeroAsiento.HasValue)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Asiento asiento = db.Asientoes.Find(modeloAeronaveId.Value, numeroAsiento.Value);
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
            var actual = db.Asientoes.Find(asiento.ModeloAeronaveId, asiento.NumeroAsiento);
            if (actual == null)
            {
                return HttpNotFound();
            }
            NormalizarAsiento(asiento);
            ValidarAsiento(asiento, asiento.NumeroAsiento);
            if (ModelState.IsValid)
            {
                actual.Codigo = asiento.Codigo.Trim();
                actual.Fila = asiento.Fila;
                actual.Lado = asiento.Lado.Trim().ToUpperInvariant();
                actual.Ubicacion = asiento.Ubicacion.Trim();
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.ModeloAeronaveId = new SelectList(db.ModeloAeronaves, "ModeloAeronaveId", "Fabricante", asiento.ModeloAeronaveId);
            return View(asiento);
        }

        // GET: Asiento/Delete/5
        public ActionResult Delete(int? modeloAeronaveId, byte? numeroAsiento)
        {
            if (!modeloAeronaveId.HasValue || !numeroAsiento.HasValue)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Asiento asiento = db.Asientoes.Find(modeloAeronaveId.Value, numeroAsiento.Value);
            if (asiento == null)
            {
                return HttpNotFound();
            }
            return View(asiento);
        }

        // POST: Asiento/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int modeloAeronaveId, byte numeroAsiento)
        {
            Asiento asiento = db.Asientoes.Find(modeloAeronaveId, numeroAsiento);
            if (asiento == null)
            {
                return HttpNotFound();
            }
            if (db.Aeronaves.Any(a => a.ModeloAeronaveId == modeloAeronaveId))
            {
                TempData["Aviso"] = "No se puede modificar el mapa de asientos de un modelo ya asignado a aeronaves.";
                return RedirectToAction("Index");
            }
            db.Asientoes.Remove(asiento);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        private void PrepararOpcionesModelo(int? seleccionado = null)
        {
            var modelos = db.ModeloAeronaves.Include(m => m.Asientoes)
                .OrderBy(m => m.Fabricante).ThenBy(m => m.Nombre).ToList()
                .Select(m => new
                {
                    m.ModeloAeronaveId,
                    Nombre = m.Fabricante + " " + m.Nombre + " (" + m.Asientoes.Count + "/" +
                        m.CapacidadAsientos + " asientos)"
                }).ToList();
            ViewBag.ModeloAeronaveId = new SelectList(modelos, "ModeloAeronaveId", "Nombre", seleccionado);
        }

        private static void NormalizarAsiento(Asiento asiento)
        {
            asiento.Codigo = asiento.Codigo == null ? null : asiento.Codigo.Trim().ToUpperInvariant();
            asiento.Lado = asiento.Lado == null ? null : asiento.Lado.Trim().ToUpperInvariant();
            asiento.Ubicacion = asiento.Ubicacion == null ? null : asiento.Ubicacion.Trim();
        }

        private void ValidarAsiento(Asiento asiento, byte? numeroOriginal)
        {
            var modelo = db.ModeloAeronaves.Find(asiento.ModeloAeronaveId);
            if (modelo == null)
            {
                ModelState.AddModelError("ModeloAeronaveId", "Seleccione un modelo existente.");
                return;
            }
            if (db.Aeronaves.Any(a => a.ModeloAeronaveId == modelo.ModeloAeronaveId))
            {
                ModelState.AddModelError("ModeloAeronaveId", "No se puede modificar el mapa de asientos de un modelo ya asignado a aeronaves.");
            }
            if (asiento.NumeroAsiento < 1 || asiento.NumeroAsiento > modelo.CapacidadAsientos)
            {
                ModelState.AddModelError("NumeroAsiento", "El numero de asiento debe estar dentro de la capacidad del modelo.");
            }
            if (asiento.Fila < 1 || asiento.Fila > 4)
            {
                ModelState.AddModelError("Fila", "La fila debe estar entre 1 y 4.");
            }
            if (!string.Equals(asiento.Lado, "D", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(asiento.Lado, "I", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError("Lado", "El lado debe ser D o I.");
            }
            if (string.IsNullOrWhiteSpace(asiento.Codigo) || asiento.Codigo.Trim().Length != 2)
            {
                ModelState.AddModelError("Codigo", "El codigo de asiento es obligatorio y debe tener dos caracteres.");
            }
            if (string.IsNullOrWhiteSpace(asiento.Ubicacion) || asiento.Ubicacion.Trim().Length > 100)
            {
                ModelState.AddModelError("Ubicacion", "La ubicacion es obligatoria y no debe superar 100 caracteres.");
            }
            if (db.Asientoes.Any(a => a.ModeloAeronaveId == asiento.ModeloAeronaveId &&
                (a.NumeroAsiento == asiento.NumeroAsiento || a.Codigo == asiento.Codigo ||
                 (a.Fila == asiento.Fila && a.Lado == asiento.Lado)) &&
                (!numeroOriginal.HasValue || a.NumeroAsiento != numeroOriginal.Value)))
            {
                ModelState.AddModelError("Codigo", "El numero, codigo o posicion del asiento ya esta registrado para este modelo.");
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
