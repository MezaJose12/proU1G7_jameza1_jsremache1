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
    [AutorizacionAdministrativa]
    [RoutePrefix("Admin/Aeronaves")]
    [Route("{action=Index}/{id?}")]
    public class AeronavesController : Controller
    {
        private AerolineaVIPEntities db = new AerolineaVIPEntities();

        // GET: Aeronaves
        public ActionResult Index()
        {
            var aeronaves = db.Aeronaves.Include(a => a.EstadoAeronave).Include(a => a.ModeloAeronave);
            ViewBag.Aviso = TempData["Aviso"] as string;
            return View(aeronaves.ToList());
        }

        // GET: Aeronaves/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Aeronave aeronave = db.Aeronaves.Find(id);
            if (aeronave == null)
            {
                return HttpNotFound();
            }
            return View(aeronave);
        }

        // GET: Aeronaves/Create
        public ActionResult Create()
        {
            PrepararListasAeronave();
            return View(new Aeronave { EstadoAeronaveCodigo = "DISPONIBLE" });
        }

        // POST: Aeronaves/Create
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "AeronaveId,Matricula,ModeloAeronaveId,EstadoAeronaveCodigo")] Aeronave aeronave)
        {
            ValidarAeronave(aeronave);
            if (ModelState.IsValid)
            {
                aeronave.Matricula = aeronave.Matricula.Trim();
                db.Aeronaves.Add(aeronave);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            PrepararListasAeronave(aeronave);
            return View(aeronave);
        }

        // GET: Aeronaves/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Aeronave aeronave = db.Aeronaves.Find(id);
            if (aeronave == null)
            {
                return HttpNotFound();
            }
            PrepararListasAeronave(aeronave);
            return View(aeronave);
        }

        // POST: Aeronaves/Edit/5
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "AeronaveId,Matricula,ModeloAeronaveId,EstadoAeronaveCodigo")] Aeronave aeronave)
        {
            var actual = db.Aeronaves.Find(aeronave.AeronaveId);
            if (actual == null)
            {
                return HttpNotFound();
            }
            if (actual.ModeloAeronaveId != aeronave.ModeloAeronaveId && db.Vueloes.Any(v => v.AeronaveId == actual.AeronaveId))
            {
                ModelState.AddModelError("ModeloAeronaveId", "No se puede cambiar el modelo de una aeronave vinculada a vuelos.");
            }
            ValidarAeronave(aeronave);
            if (ModelState.IsValid)
            {
                actual.Matricula = aeronave.Matricula.Trim();
                actual.ModeloAeronaveId = aeronave.ModeloAeronaveId;
                actual.EstadoAeronaveCodigo = aeronave.EstadoAeronaveCodigo;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            PrepararListasAeronave(aeronave);
            return View(aeronave);
        }


        // GET: Aeronaves/Delete/5
        [Authorize(Roles = "Administrador,SuperUsuario")]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Aeronave aeronave = db.Aeronaves.Find(id);
            if (aeronave == null)
            {
                return HttpNotFound();
            }
            return View(aeronave);
        }

        // POST: Aeronaves/Delete/5
        [Authorize(Roles = "Administrador,SuperUsuario")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Aeronave aeronave = db.Aeronaves.Find(id);
            if (aeronave == null)
            {
                return HttpNotFound();
            }
            if (db.Vueloes.Any(v => v.AeronaveId == id))
            {
                TempData["Aviso"] = "La aeronave tiene vuelos vinculados y no se puede eliminar. Cambie su estado a NO_DISPONIBLE.";
                return RedirectToAction("Index");
            }
            db.Aeronaves.Remove(aeronave);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        private void ValidarAeronave(Aeronave aeronave)
        {
            var matricula = aeronave.Matricula == null ? string.Empty : aeronave.Matricula.Trim();
            if (string.IsNullOrWhiteSpace(matricula))
            {
                ModelState.AddModelError("Matricula", "La matricula es obligatoria.");
            }
            else if (db.Aeronaves.Any(a => a.Matricula == matricula && a.AeronaveId != aeronave.AeronaveId))
            {
                ModelState.AddModelError("Matricula", "Ya existe una aeronave con esa matricula.");
            }
            var modelo = db.ModeloAeronaves.Include(m => m.Asientoes)
                .FirstOrDefault(m => m.ModeloAeronaveId == aeronave.ModeloAeronaveId);
            if (modelo == null)
            {
                ModelState.AddModelError("ModeloAeronaveId", "Seleccione un modelo existente.");
            }
            else if (modelo.Asientoes.Count != modelo.CapacidadAsientos ||
                modelo.Asientoes.Any(s => s.NumeroAsiento > modelo.CapacidadAsientos))
            {
                ModelState.AddModelError("ModeloAeronaveId", "Configure todos los asientos del modelo antes de registrar una aeronave.");
            }
            if (string.IsNullOrWhiteSpace(aeronave.EstadoAeronaveCodigo) ||
                !db.EstadoAeronaves.Any(e => e.Codigo == aeronave.EstadoAeronaveCodigo))
            {
                ModelState.AddModelError("EstadoAeronaveCodigo", "Seleccione un estado de aeronave valido.");
            }
        }

        private void PrepararListasAeronave(Aeronave aeronave = null)
        {
            ViewBag.EstadoAeronaveCodigo = new SelectList(db.EstadoAeronaves, "Codigo", "Descripcion",
                aeronave == null ? null : aeronave.EstadoAeronaveCodigo);
            var modelos = db.ModeloAeronaves.Include(m => m.Asientoes).OrderBy(m => m.Fabricante).ThenBy(m => m.Nombre)
                .ToList()
                .Where(m => m.Asientoes.Count == m.CapacidadAsientos &&
                    !m.Asientoes.Any(s => s.NumeroAsiento > m.CapacidadAsientos))
                .Select(m => new
                {
                    m.ModeloAeronaveId,
                    Nombre = m.Fabricante + " " + m.Nombre + " (" + m.CapacidadAsientos + " asientos)"
                }).ToList();
            ViewBag.ModeloAeronaveId = new SelectList(modelos, "ModeloAeronaveId", "Nombre",
                aeronave == null ? (int?)null : aeronave.ModeloAeronaveId);
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
