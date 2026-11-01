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
    public class TipoDocumentoIdentidadsController : Controller
    {
        private AerolineaVIPEntities db = new AerolineaVIPEntities();

        // GET: TipoDocumentoIdentidads
        public ActionResult Index()
        {
            return View(db.TipoDocumentoIdentidads.ToList());
        }

        // GET: TipoDocumentoIdentidads/Details/5
        public ActionResult Details(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            TipoDocumentoIdentidad tipoDocumentoIdentidad = db.TipoDocumentoIdentidads.Find(id);
            if (tipoDocumentoIdentidad == null)
            {
                return HttpNotFound();
            }
            return View(tipoDocumentoIdentidad);
        }

        // GET: TipoDocumentoIdentidads/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: TipoDocumentoIdentidads/Create
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Codigo,Descripcion")] TipoDocumentoIdentidad tipoDocumentoIdentidad)
        {
            if (ModelState.IsValid)
            {
                db.TipoDocumentoIdentidads.Add(tipoDocumentoIdentidad);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(tipoDocumentoIdentidad);
        }

        // GET: TipoDocumentoIdentidads/Edit/5
        public ActionResult Edit(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            TipoDocumentoIdentidad tipoDocumentoIdentidad = db.TipoDocumentoIdentidads.Find(id);
            if (tipoDocumentoIdentidad == null)
            {
                return HttpNotFound();
            }
            return View(tipoDocumentoIdentidad);
        }

        // POST: TipoDocumentoIdentidads/Edit/5
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Codigo,Descripcion")] TipoDocumentoIdentidad tipoDocumentoIdentidad)
        {
            if (ModelState.IsValid)
            {
                db.Entry(tipoDocumentoIdentidad).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(tipoDocumentoIdentidad);
        }

        // GET: TipoDocumentoIdentidads/Delete/5
        public ActionResult Delete(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            TipoDocumentoIdentidad tipoDocumentoIdentidad = db.TipoDocumentoIdentidads.Find(id);
            if (tipoDocumentoIdentidad == null)
            {
                return HttpNotFound();
            }
            return View(tipoDocumentoIdentidad);
        }

        // POST: TipoDocumentoIdentidads/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(string id)
        {
            TipoDocumentoIdentidad tipoDocumentoIdentidad = db.TipoDocumentoIdentidads.Find(id);
            db.TipoDocumentoIdentidads.Remove(tipoDocumentoIdentidad);
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
