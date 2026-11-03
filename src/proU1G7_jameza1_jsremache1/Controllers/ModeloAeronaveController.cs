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
    // CAMBIO: restringir la administración de modelos de aeronave.
    [Authorize(Roles = "Administrador,SuperUsuario")]
    public class ModeloAeronaveController : Controller
    {
        private AerolineaVIPEntities db = new AerolineaVIPEntities();

        // GET: ModeloAeronave
        public ActionResult Index()
        {
            ViewBag.Aviso = TempData["Aviso"] as string;
            return View(db.ModeloAeronaves.Include(m => m.Asientoes).ToList());
        }

        // GET: ModeloAeronave/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ModeloAeronave modeloAeronave = db.ModeloAeronaves.Find(id);
            if (modeloAeronave == null)
            {
                return HttpNotFound();
            }
            return View(modeloAeronave);
        }

        // GET: ModeloAeronave/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: ModeloAeronave/Create
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "ModeloAeronaveId,Fabricante,Nombre,CapacidadAsientos")] ModeloAeronave modeloAeronave)
        {
            ValidarModelo(modeloAeronave);
            if (ModelState.IsValid)
            {
                modeloAeronave.Fabricante = modeloAeronave.Fabricante.Trim();
                modeloAeronave.Nombre = modeloAeronave.Nombre.Trim();
                db.ModeloAeronaves.Add(modeloAeronave);
                db.SaveChanges();
                return RedirectToAction("Create", "Asiento", new { modeloAeronaveId = modeloAeronave.ModeloAeronaveId });
            }

            return View(modeloAeronave);
        }

        // GET: ModeloAeronave/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ModeloAeronave modeloAeronave = db.ModeloAeronaves.Find(id);
            if (modeloAeronave == null)
            {
                return HttpNotFound();
            }
            return View(modeloAeronave);
        }

        // POST: ModeloAeronave/Edit/5
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "ModeloAeronaveId,Fabricante,Nombre,CapacidadAsientos")] ModeloAeronave modeloAeronave)
        {
            var actual = db.ModeloAeronaves.Find(modeloAeronave.ModeloAeronaveId);
            if (actual == null)
            {
                return HttpNotFound();
            }
            var tieneAeronaves = db.Aeronaves.Any(a => a.ModeloAeronaveId == actual.ModeloAeronaveId);
            if (tieneAeronaves && actual.CapacidadAsientos != modeloAeronave.CapacidadAsientos)
            {
                ModelState.AddModelError("CapacidadAsientos", "No se puede cambiar la capacidad de un modelo ya asignado a aeronaves.");
            }
            ValidarModelo(modeloAeronave);
            if (ModelState.IsValid)
            {
                actual.Fabricante = modeloAeronave.Fabricante.Trim();
                actual.Nombre = modeloAeronave.Nombre.Trim();
                actual.CapacidadAsientos = modeloAeronave.CapacidadAsientos;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(modeloAeronave);
        }

        // GET: ModeloAeronave/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ModeloAeronave modeloAeronave = db.ModeloAeronaves.Find(id);
            if (modeloAeronave == null)
            {
                return HttpNotFound();
            }
            return View(modeloAeronave);
        }

        // POST: ModeloAeronave/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            ModeloAeronave modeloAeronave = db.ModeloAeronaves.Find(id);
            if (modeloAeronave == null)
            {
                return HttpNotFound();
            }
            if (db.Aeronaves.Any(a => a.ModeloAeronaveId == id) || db.Asientoes.Any(a => a.ModeloAeronaveId == id))
            {
                TempData["Aviso"] = "El modelo tiene aeronaves o asientos vinculados y no se puede eliminar.";
                return RedirectToAction("Index");
            }
            db.ModeloAeronaves.Remove(modeloAeronave);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        private void ValidarModelo(ModeloAeronave modelo)
        {
            var fabricante = modelo.Fabricante == null ? string.Empty : modelo.Fabricante.Trim();
            var nombre = modelo.Nombre == null ? string.Empty : modelo.Nombre.Trim();
            if (string.IsNullOrWhiteSpace(fabricante))
            {
                ModelState.AddModelError("Fabricante", "El fabricante es obligatorio.");
            }
            if (string.IsNullOrWhiteSpace(nombre))
            {
                ModelState.AddModelError("Nombre", "El nombre del modelo es obligatorio.");
            }
            if (fabricante.Length > 60)
            {
                ModelState.AddModelError("Fabricante", "El fabricante no debe superar 60 caracteres.");
            }
            if (nombre.Length > 60)
            {
                ModelState.AddModelError("Nombre", "El nombre del modelo no debe superar 60 caracteres.");
            }
            if (modelo.CapacidadAsientos < 1 || modelo.CapacidadAsientos > 7)
            {
                ModelState.AddModelError("CapacidadAsientos", "La capacidad debe estar entre 1 y 7 asientos.");
            }
            if (!string.IsNullOrWhiteSpace(fabricante) && !string.IsNullOrWhiteSpace(nombre) &&
                db.ModeloAeronaves.Any(m => m.Fabricante == fabricante && m.Nombre == nombre &&
                    m.ModeloAeronaveId != modelo.ModeloAeronaveId))
            {
                ModelState.AddModelError("Nombre", "Ya existe un modelo con ese fabricante y nombre.");
            }
            var cantidadAsientos = db.Asientoes.Count(a => a.ModeloAeronaveId == modelo.ModeloAeronaveId);
            if (cantidadAsientos > 0 && (cantidadAsientos != modelo.CapacidadAsientos ||
                db.Asientoes.Any(a => a.ModeloAeronaveId == modelo.ModeloAeronaveId && a.NumeroAsiento > modelo.CapacidadAsientos)))
            {
                ModelState.AddModelError("CapacidadAsientos", "La capacidad debe coincidir con la cantidad y numeracion de los asientos configurados.");
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
