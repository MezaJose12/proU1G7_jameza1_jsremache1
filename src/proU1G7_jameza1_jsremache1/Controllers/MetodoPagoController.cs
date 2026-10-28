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
    public class MetodoPagoController : Controller
    {
        private AerolineaVIPEntities db = new AerolineaVIPEntities();

        // GET: MetodoPago
        public ActionResult Index()
        {
            return View(db.MetodoPagoes.ToList());
        }

        // GET: MetodoPago/Details/5
        public ActionResult Details(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            MetodoPago metodoPago = db.MetodoPagoes.Find(id);
            if (metodoPago == null)
            {
                return HttpNotFound();
            }
            return View(metodoPago);
        }

        // GET: MetodoPago/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: MetodoPago/Create
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Codigo,Nombre,Activo")] MetodoPago metodoPago)
        {
            if (ModelState.IsValid)
            {
                db.MetodoPagoes.Add(metodoPago);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(metodoPago);
        }

        // GET: MetodoPago/Edit/5
        public ActionResult Edit(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            MetodoPago metodoPago = db.MetodoPagoes.Find(id);
            if (metodoPago == null)
            {
                return HttpNotFound();
            }
            return View(metodoPago);
        }

        // POST: MetodoPago/Edit/5
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Codigo,Nombre,Activo")] MetodoPago metodoPago)
        {
            if (ModelState.IsValid)
            {
                db.Entry(metodoPago).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(metodoPago);
        }

        // GET: MetodoPago/Delete/5
        public ActionResult Delete(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            MetodoPago metodoPago = db.MetodoPagoes.Find(id);
            if (metodoPago == null)
            {
                return HttpNotFound();
            }
            return View(metodoPago);
        }

        // POST: MetodoPago/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(string id)
        {
            MetodoPago metodoPago = db.MetodoPagoes.Find(id);
            db.MetodoPagoes.Remove(metodoPago);
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
