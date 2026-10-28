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
    public class ModeloAeronaveController : Controller
    {
        private AerolineaVIPEntities db = new AerolineaVIPEntities();

        // GET: ModeloAeronave
        public ActionResult Index()
        {
            return View(db.ModeloAeronaves.ToList());
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
            if (ModelState.IsValid)
            {
                db.ModeloAeronaves.Add(modeloAeronave);
                db.SaveChanges();
                return RedirectToAction("Index");
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
            if (ModelState.IsValid)
            {
                db.Entry(modeloAeronave).State = EntityState.Modified;
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
            db.ModeloAeronaves.Remove(modeloAeronave);
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
