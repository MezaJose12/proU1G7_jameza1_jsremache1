using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using proU1G7_jameza1_jsremache1.Models;

namespace proU1G7_jameza1_jsremache1.Controllers
{
    public class HomeController : Controller
    {
        private readonly AerolineaVIPEntities db = new AerolineaVIPEntities();

        public ActionResult Index()
        {
            var aeropuertos = db.Aeropuertoes
                .Where(a => a.Activo)
                .OrderBy(a => a.Localidad)
                .ToList()
                .Select(a => new SelectListItem
                {
                    Value = a.CodigoIATA,
                    Text = a.Localidad + " (" + a.CodigoIATA + ")"
                })
                .ToList();
            var destinoIds = db.Rutas
                .Where(r => r.Activa)
                .Select(r => r.AeropuertoDestinoId)
                .Distinct()
                .ToList();
            var destinos = db.Aeropuertoes
                .Where(a => a.Activo && destinoIds.Contains(a.AeropuertoId))
                .OrderBy(a => a.Localidad)
                .ToList()
                .Select(a => new SelectListItem
                {
                    Value = a.CodigoIATA,
                    Text = a.Localidad + " (" + a.CodigoIATA + ")"
                })
                .ToList();
            var model = new InterfacesViewModel
            {
                Aeropuertos = aeropuertos,
                Destinos = destinos,
                CantidadPasajeros = 1
            };
            return View(model);
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
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