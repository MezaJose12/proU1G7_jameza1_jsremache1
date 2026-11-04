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
    // CAMBIO: restringir la programación de vuelos al personal administrativo.
    [AutorizacionAdministrativa]
    public class FrecuenciaController : Controller
    {
        private AerolineaVIPEntities db = new AerolineaVIPEntities();

        // GET: Frecuencia
        public ActionResult Index()
        {
            var frecuencias = db.Frecuencias
                .Include(f => f.Ruta.Aeropuerto)
                .Include(f => f.Ruta.Aeropuerto1)
                .OrderBy(f => f.DiaSemana)
                .ThenBy(f => f.HoraSalida);
            ViewBag.Aviso = TempData["Aviso"] as string;
            return View(frecuencias.ToList());
        }

        // GET: Frecuencia/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Frecuencia frecuencia = db.Frecuencias.Find(id);
            if (frecuencia == null)
            {
                return HttpNotFound();
            }
            return View(frecuencia);
        }

        // GET: Frecuencia/Create
        public ActionResult Create()
        {
            ViewBag.RutaId = CrearOpcionesRuta();
            ViewBag.DiaSemana = CrearOpcionesDia();
            return View(new Frecuencia { Activa = true });
        }

        // POST: Frecuencia/Create
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "FrecuenciaId,RutaId,DiaSemana,HoraSalida,HoraLlegada,Activa")] Frecuencia frecuencia)
        {
            ValidarFrecuencia(frecuencia);
            if (ModelState.IsValid)
            {
                db.Frecuencias.Add(frecuencia);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.RutaId = CrearOpcionesRuta(frecuencia.RutaId);
            ViewBag.DiaSemana = CrearOpcionesDia(frecuencia.DiaSemana);
            return View(frecuencia);
        }

        // GET: Frecuencia/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Frecuencia frecuencia = db.Frecuencias.Find(id);
            if (frecuencia == null)
            {
                return HttpNotFound();
            }
            ViewBag.RutaId = CrearOpcionesRuta(frecuencia.RutaId);
            ViewBag.DiaSemana = CrearOpcionesDia(frecuencia.DiaSemana);
            return View(frecuencia);
        }

        // POST: Frecuencia/Edit/5
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "FrecuenciaId,RutaId,DiaSemana,HoraSalida,HoraLlegada,Activa")] Frecuencia frecuencia)
        {
            var actual = db.Frecuencias.Find(frecuencia.FrecuenciaId);
            if (actual == null)
            {
                return HttpNotFound();
            }
            var tieneVuelos = db.Vueloes.Any(v => v.FrecuenciaId == actual.FrecuenciaId);
            if (tieneVuelos && (actual.RutaId != frecuencia.RutaId || actual.DiaSemana != frecuencia.DiaSemana ||
                actual.HoraSalida != frecuencia.HoraSalida || actual.HoraLlegada != frecuencia.HoraLlegada))
            {
                ModelState.AddModelError("", "Esta frecuencia ya tiene vuelos. Para conservar horarios e historial, desactivela y cree una nueva frecuencia para el nuevo horario.");
            }
            ValidarFrecuencia(frecuencia);
            if (ModelState.IsValid)
            {
                actual.Activa = frecuencia.Activa;
                if (!tieneVuelos)
                {
                    actual.RutaId = frecuencia.RutaId;
                    actual.DiaSemana = frecuencia.DiaSemana;
                    actual.HoraSalida = frecuencia.HoraSalida;
                    actual.HoraLlegada = frecuencia.HoraLlegada;
                }
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.RutaId = CrearOpcionesRuta(frecuencia.RutaId);
            ViewBag.DiaSemana = CrearOpcionesDia(frecuencia.DiaSemana);
            return View(frecuencia);
        }

        // GET: Frecuencia/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Frecuencia frecuencia = db.Frecuencias.Find(id);
            if (frecuencia == null)
            {
                return HttpNotFound();
            }
            return View(frecuencia);
        }

        // POST: Frecuencia/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Frecuencia frecuencia = db.Frecuencias.Find(id);
            if (frecuencia == null)
            {
                return HttpNotFound();
            }
            if (db.Vueloes.Any(v => v.FrecuenciaId == id))
            {
                frecuencia.Activa = false;
                TempData["Aviso"] = "La frecuencia se desactivo porque tiene vuelos asociados; su historial se conserva.";
            }
            else
            {
                db.Frecuencias.Remove(frecuencia);
            }
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        private void ValidarFrecuencia(Frecuencia frecuencia)
        {
            foreach (var campo in new[] { "DiaSemana", "HoraSalida", "HoraLlegada" })
            {
                if (!ModelState.ContainsKey(campo) || ModelState[campo].Value == null ||
                    string.IsNullOrWhiteSpace(ModelState[campo].Value.AttemptedValue))
                {
                    ModelState.AddModelError(campo, "Este campo es obligatorio.");
                }
            }
            if (!db.Rutas.Any(r => r.RutaId == frecuencia.RutaId))
            {
                ModelState.AddModelError("RutaId", "Seleccione una ruta existente.");
            }
            if (frecuencia.DiaSemana < 1 || frecuencia.DiaSemana > 7)
            {
                ModelState.AddModelError("DiaSemana", "Seleccione un dia entre lunes y domingo.");
            }
            if (frecuencia.HoraLlegada <= frecuencia.HoraSalida)
            {
                ModelState.AddModelError("HoraLlegada", "La hora de llegada debe ser posterior a la salida en el mismo dia.");
            }
            if (frecuencia.Activa && !db.Rutas.Any(r => r.RutaId == frecuencia.RutaId && r.Activa))
            {
                ModelState.AddModelError("RutaId", "Seleccione una ruta activa.");
            }
            if (db.Frecuencias.Any(f => f.RutaId == frecuencia.RutaId && f.DiaSemana == frecuencia.DiaSemana &&
                f.HoraSalida == frecuencia.HoraSalida && f.FrecuenciaId != frecuencia.FrecuenciaId))
            {
                ModelState.AddModelError("HoraSalida", "Ya existe una frecuencia con esta ruta, dia y hora de salida.");
            }
        }

        private SelectList CrearOpcionesDia(byte? seleccionada = null)
        {
            var dias = new[]
            {
                new { Dia = (byte)1, Nombre = "Lunes" },
                new { Dia = (byte)2, Nombre = "Martes" },
                new { Dia = (byte)3, Nombre = "Miercoles" },
                new { Dia = (byte)4, Nombre = "Jueves" },
                new { Dia = (byte)5, Nombre = "Viernes" },
                new { Dia = (byte)6, Nombre = "Sabado" },
                new { Dia = (byte)7, Nombre = "Domingo" }
            };
            return new SelectList(dias, "Dia", "Nombre", seleccionada);
        }

        private SelectList CrearOpcionesRuta(int? seleccionada = null)
        {
            var rutas = db.Rutas
                .Include(r => r.Aeropuerto)
                .Include(r => r.Aeropuerto1)
                .OrderBy(r => r.Aeropuerto.CodigoIATA)
                .ToList()
                .Select(r => new
                {
                    r.RutaId,
                    Nombre = r.Aeropuerto.CodigoIATA + " - " + r.Aeropuerto1.CodigoIATA +
                        " (Tarifa " + r.TarifaReferencial.ToString("0.00") + " USD)" + (r.Activa ? "" : " (INACTIVA)")
                }).ToList();
            return new SelectList(rutas, "RutaId", "Nombre", seleccionada);
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
