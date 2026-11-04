using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using proU1G7_jameza1_jsremache1.Models;

namespace proU1G7_jameza1_jsremache1.Controllers
{
    // CAMBIO: restringir la administración de itinerarios.
    [Authorize(Roles = "Administrador,SuperUsuario")]
    public class ItinerarioController : Controller
    {
        private AerolineaVIPEntities db = new AerolineaVIPEntities();

        // GET: Itinerario
        public ActionResult Index()
        {
            var itinerarios = db.Itinerarios
                .Include(i => i.EstadoItinerario)
                .Include(i => i.TramoItinerarios);
            ViewBag.Aviso = TempData["Aviso"] as string;
            return View(itinerarios.ToList());
        }

        // GET: Itinerario/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Itinerario itinerario = db.Itinerarios.Find(id);
            if (itinerario == null)
            {
                return HttpNotFound();
            }
            return View(itinerario);
        }

        // GET: Itinerario/Create
        public ActionResult Create()
        {
            ViewBag.EstadoItinerarioCodigo = new SelectList(db.EstadoItinerarios, "Codigo", "Descripcion");
            return View();
        }

        // POST: Itinerario/Create
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "ItinerarioId,Referencia,Nombre,EstadoItinerarioCodigo")] Itinerario itinerario)
        {
            if (ModelState.IsValid)
            {
                db.Itinerarios.Add(itinerario);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.EstadoItinerarioCodigo = new SelectList(db.EstadoItinerarios, "Codigo", "Descripcion", itinerario.EstadoItinerarioCodigo);
            return View(itinerario);
        }

        // GET: Itinerario/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Itinerario itinerario = db.Itinerarios.Find(id);
            if (itinerario == null)
            {
                return HttpNotFound();
            }
            ViewBag.EstadoItinerarioCodigo = new SelectList(db.EstadoItinerarios, "Codigo", "Descripcion", itinerario.EstadoItinerarioCodigo);
            return View(itinerario);
        }

        // POST: Itinerario/Edit/5
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "ItinerarioId,Referencia,Nombre,EstadoItinerarioCodigo")] Itinerario itinerario)
        {
            if (ModelState.IsValid)
            {
                db.Entry(itinerario).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.EstadoItinerarioCodigo = new SelectList(db.EstadoItinerarios, "Codigo", "Descripcion", itinerario.EstadoItinerarioCodigo);
            return View(itinerario);
        }

        // GET: Itinerario/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Itinerario itinerario = db.Itinerarios.Find(id);
            if (itinerario == null)
            {
                return HttpNotFound();
            }
            return View(itinerario);
        }

        // POST: Itinerario/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Itinerario itinerario = db.Itinerarios.Find(id);
            if (itinerario == null)
            {
                return HttpNotFound();
            }

            if (db.TramoItinerarios.Any(t => t.ItinerarioId == id))
            {
                TempData["Aviso"] = "No se puede eliminar el itinerario porque tiene tramos asociados.";
                return RedirectToAction("Index");
            }

            db.Itinerarios.Remove(itinerario);
            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateException ex)
            {
                if (!EsViolacionDeClaveForanea(ex))
                {
                    throw;
                }

                TempData["Aviso"] = "No se puede eliminar el itinerario porque tiene tramos asociados.";
            }

            return RedirectToAction("Index");
        }

        private static bool EsViolacionDeClaveForanea(Exception exception)
        {
            for (Exception actual = exception; actual != null; actual = actual.InnerException)
            {
                SqlException errorSql = actual as SqlException;
                if (errorSql != null && errorSql.Number == 547)
                {
                    return true;
                }
            }

            return false;
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
