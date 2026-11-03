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
    // CAMBIO: restringir la creación y edición del inventario de vuelos.
    [Authorize(Roles = "Administrador,SuperUsuario")]
    public class VueloController : Controller
    {
        private AerolineaVIPEntities db = new AerolineaVIPEntities();

        // CAMBIO: permite consultar vuelos por fechas sin alterar las acciones CRUD scaffolded.
        public ActionResult Index(DateTime? desde, DateTime? hasta)
        {
            var fechaDesde = desde.HasValue ? desde.Value.Date : (DateTime?)null;
            var fechaHastaExclusiva = hasta.HasValue ? hasta.Value.Date.AddDays(1) : (DateTime?)null;

            IQueryable<Vuelo> vueloes = db.Vueloes
                .Include(v => v.Aeronave)
                .Include(v => v.EstadoVuelo)
                .Include(v => v.Frecuencia.Ruta.Aeropuerto)
                .Include(v => v.Frecuencia.Ruta.Aeropuerto1);
            if (fechaDesde.HasValue)
            {
                vueloes = vueloes.Where(v => v.FechaOperacion >= fechaDesde.Value);
            }
            if (fechaHastaExclusiva.HasValue)
            {
                vueloes = vueloes.Where(v => v.FechaOperacion < fechaHastaExclusiva.Value);
            }

            ViewBag.Desde = desde.HasValue ? desde.Value.ToString("yyyy-MM-dd") : string.Empty;
            ViewBag.Hasta = hasta.HasValue ? hasta.Value.ToString("yyyy-MM-dd") : string.Empty;
            ViewBag.DesdeGenerar = desde.HasValue ? desde.Value.ToString("yyyy-MM-dd") : DateTime.Today.ToString("yyyy-MM-dd");
            ViewBag.HastaGenerar = hasta.HasValue ? hasta.Value.ToString("yyyy-MM-dd") : DateTime.Today.AddDays(60).ToString("yyyy-MM-dd");
            ViewBag.Aviso = TempData["Aviso"] as string;
            return View(vueloes.OrderBy(v => v.FechaOperacion).ThenBy(v => v.Frecuencia.HoraSalida).ToList());
        }

        // CAMBIO DP-02: genera ocurrencias semanales de frecuencias activas sin asignar aeronaves automaticamente.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Generar(DateTime? desde, DateTime? hasta)
        {
            if (!desde.HasValue || !hasta.HasValue || hasta.Value.Date < desde.Value.Date ||
                (hasta.Value.Date - desde.Value.Date).TotalDays > 365)
            {
                TempData["Aviso"] = "Seleccione un rango valido de hasta 366 dias.";
                return RedirectToAction("Index");
            }

            var inicio = desde.Value.Date;
            var fin = hasta.Value.Date;
            using (var transaction = db.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                var existentesAntes = ContarOcurrenciasDeFrecuenciasActivas(inicio, fin);
                db.Database.ExecuteSqlCommand(
                    "EXEC dbo.usp_GenerarVuelos @FechaInicio = @p0, @FechaFin = @p1",
                    inicio, fin);
                var existentesDespues = ContarOcurrenciasDeFrecuenciasActivas(inicio, fin);
                transaction.Commit();
                TempData["Aviso"] = (existentesDespues - existentesAntes) +
                    " vuelos semanales generados. Las aeronaves quedan sin asignar para validacion administrativa.";
            }

            return RedirectToAction("Index", new { desde = inicio.ToString("yyyy-MM-dd"), hasta = fin.ToString("yyyy-MM-dd") });
        }

        private int ContarOcurrenciasDeFrecuenciasActivas(DateTime inicio, DateTime fin)
        {
            var ocurrencias = db.Vueloes
                .Where(v => v.FechaOperacion >= inicio && v.FechaOperacion <= fin &&
                    v.Frecuencia.Activa && v.Frecuencia.Ruta.Activa)
                .Select(v => new { v.FechaOperacion, v.Frecuencia.DiaSemana })
                .ToList();
            return ocurrencias.Count(v => v.DiaSemana == (((int)v.FechaOperacion.DayOfWeek + 6) % 7) + 1);
        }

        // GET: Vuelo/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Vuelo vuelo = db.Vueloes.Find(id);
            if (vuelo == null)
            {
                return HttpNotFound();
            }
            return View(vuelo);
        }

        // GET: Vuelo/Create
        public ActionResult Create()
        {
            ViewBag.AeronaveId = CrearOpcionesAeronave();
            ViewBag.EstadoVueloCodigo = new SelectList(db.EstadoVueloes, "Codigo", "Descripcion", "PROGRAMADO");
            ViewBag.FrecuenciaId = CrearOpcionesFrecuencia();
            return View(new Vuelo { EstadoVueloCodigo = "PROGRAMADO" });
        }

        // POST: Vuelo/Create
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "VueloId,FrecuenciaId,FechaOperacion,AeronaveId,EstadoVueloCodigo")] Vuelo vuelo)
        {
            ValidarFrecuenciaParaVuelo(vuelo);
            ValidarAeronaveParaVuelo(vuelo.AeronaveId);
            ValidarConflictoHorarioAeronave(vuelo, null);
            ValidarEstadoVuelo(vuelo.EstadoVueloCodigo);

            if (ModelState.IsValid)
            {
                db.Vueloes.Add(vuelo);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.AeronaveId = CrearOpcionesAeronave(vuelo.AeronaveId);
            ViewBag.EstadoVueloCodigo = new SelectList(db.EstadoVueloes, "Codigo", "Descripcion", vuelo.EstadoVueloCodigo);
            ViewBag.FrecuenciaId = CrearOpcionesFrecuencia(vuelo.FrecuenciaId);
            return View(vuelo);
        }

        // GET: Vuelo/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Vuelo vuelo = db.Vueloes.Find(id);
            if (vuelo == null)
            {
                return HttpNotFound();
            }
            ViewBag.AeronaveId = CrearOpcionesAeronave(vuelo.AeronaveId);
            ViewBag.EstadoVueloCodigo = new SelectList(db.EstadoVueloes, "Codigo", "Descripcion", vuelo.EstadoVueloCodigo);
            ViewBag.FrecuenciaId = CrearOpcionesFrecuencia(vuelo.FrecuenciaId);
            return View(vuelo);
        }

        // POST: Vuelo/Edit/5
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "VueloId,FrecuenciaId,FechaOperacion,AeronaveId,EstadoVueloCodigo")] Vuelo vuelo)
        {
            var actual = db.Vueloes.Find(vuelo.VueloId);
            if (actual == null)
            {
                return HttpNotFound();
            }
            ValidarFrecuenciaParaVuelo(vuelo);
            var tieneAsignaciones = db.ReservaPasajeroVueloes.Any(a => a.VueloId == actual.VueloId) ||
                db.TramoItinerarios.Any(t => t.VueloId == actual.VueloId);
            if (tieneAsignaciones && (actual.FrecuenciaId != vuelo.FrecuenciaId || actual.FechaOperacion != vuelo.FechaOperacion))
            {
                ModelState.AddModelError("", "El vuelo ya esta vinculado a reservas o itinerarios. No se pueden cambiar su fecha ni frecuencia.");
            }
            if (actual.EstadoVueloCodigo == "COMPLETADO" && actual.AeronaveId != vuelo.AeronaveId)
            {
                ModelState.AddModelError("AeronaveId", "DP-02: no se puede cambiar la aeronave de un vuelo completado.");
            }
            ValidarAeronaveParaVuelo(vuelo.AeronaveId, actual.AeronaveId);
            ValidarConflictoHorarioAeronave(vuelo, actual.VueloId);
            ValidarEstadoVuelo(vuelo.EstadoVueloCodigo);

            if (ModelState.IsValid)
            {
                actual.FrecuenciaId = vuelo.FrecuenciaId;
                actual.FechaOperacion = vuelo.FechaOperacion;
                actual.AeronaveId = vuelo.AeronaveId;
                actual.EstadoVueloCodigo = vuelo.EstadoVueloCodigo;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.AeronaveId = CrearOpcionesAeronave(vuelo.AeronaveId);
            ViewBag.EstadoVueloCodigo = new SelectList(db.EstadoVueloes, "Codigo", "Descripcion", vuelo.EstadoVueloCodigo);
            ViewBag.FrecuenciaId = CrearOpcionesFrecuencia(vuelo.FrecuenciaId);
            return View(vuelo);
        }

        // GET: Vuelo/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Vuelo vuelo = db.Vueloes.Find(id);
            if (vuelo == null)
            {
                return HttpNotFound();
            }
            return View(vuelo);
        }

        // POST: Vuelo/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Vuelo vuelo = db.Vueloes.Find(id);
            if (vuelo == null)
            {
                return HttpNotFound();
            }
            if (db.ReservaPasajeroVueloes.Any(a => a.VueloId == id) || db.TramoItinerarios.Any(t => t.VueloId == id))
            {
                TempData["Aviso"] = "El vuelo tiene reservas o itinerarios asociados y no se puede eliminar. Desactive la frecuencia para evitar nuevas ocurrencias.";
                return RedirectToAction("Index");
            }
            db.Vueloes.Remove(vuelo);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        private void ValidarFrecuenciaParaVuelo(Vuelo vuelo)
        {
            var frecuencia = db.Frecuencias.Include(f => f.Ruta)
                .FirstOrDefault(f => f.FrecuenciaId == vuelo.FrecuenciaId);
            if (frecuencia == null || !frecuencia.Activa || !frecuencia.Ruta.Activa)
            {
                ModelState.AddModelError("FrecuenciaId", "Seleccione una frecuencia y ruta activas.");
                return;
            }
            if (!ModelState.ContainsKey("FechaOperacion") || ModelState["FechaOperacion"].Value == null ||
                string.IsNullOrWhiteSpace(ModelState["FechaOperacion"].Value.AttemptedValue))
            {
                ModelState.AddModelError("FechaOperacion", "La fecha de operacion es obligatoria.");
                return;
            }
            var diaVuelo = (byte)(((int)vuelo.FechaOperacion.DayOfWeek + 6) % 7 + 1);
            if (diaVuelo != frecuencia.DiaSemana)
            {
                ModelState.AddModelError("FechaOperacion", "La fecha debe coincidir con el dia semanal de la frecuencia seleccionada.");
            }
            if (db.Vueloes.Any(v => v.FrecuenciaId == vuelo.FrecuenciaId &&
                v.FechaOperacion == vuelo.FechaOperacion && v.VueloId != vuelo.VueloId))
            {
                ModelState.AddModelError("FechaOperacion", "Ya existe un vuelo para esta frecuencia en la fecha seleccionada.");
            }
        }

        private void ValidarAeronaveParaVuelo(int? aeronaveId, int? aeronaveActualId = null)
        {
            if (!aeronaveId.HasValue)
            {
                ModelState.AddModelError("AeronaveId", "Asigne una aeronave antes de guardar el vuelo.");
                return;
            }

            var aeronave = db.Aeronaves.Include(a => a.ModeloAeronave.Asientoes)
                .FirstOrDefault(a => a.AeronaveId == aeronaveId.Value);
            if (aeronave == null)
            {
                ModelState.AddModelError("AeronaveId", "Seleccione una aeronave existente.");
                return;
            }
            if (aeronave.EstadoAeronaveCodigo != "DISPONIBLE" && aeronave.AeronaveId != aeronaveActualId)
            {
                ModelState.AddModelError("AeronaveId", "Seleccione una aeronave disponible.");
            }
            if (aeronave.ModeloAeronave.Asientoes.Count != aeronave.ModeloAeronave.CapacidadAsientos ||
                aeronave.ModeloAeronave.Asientoes.Any(s => s.NumeroAsiento > aeronave.ModeloAeronave.CapacidadAsientos))
            {
                ModelState.AddModelError("AeronaveId", "El modelo de la aeronave no tiene todos sus asientos configurados.");
            }
        }

        private void ValidarConflictoHorarioAeronave(Vuelo vuelo, int? vueloActualId)
        {
            if (!vuelo.AeronaveId.HasValue || !ModelState.IsValidField("FechaOperacion"))
            {
                return;
            }
            var frecuencia = db.Frecuencias.Include(f => f.Ruta)
                .FirstOrDefault(f => f.FrecuenciaId == vuelo.FrecuenciaId);
            if (frecuencia == null)
            {
                return;
            }
            var preparacion = db.Database.SqlQuery<int?>(
                "SELECT CAST(Valor AS INT) FROM dbo.ParametroPolitica WHERE Codigo = @p0", "PreparacionAeronaveMinutos").FirstOrDefault();
            var liberacion = db.Database.SqlQuery<int?>(
                "SELECT CAST(Valor AS INT) FROM dbo.ParametroPolitica WHERE Codigo = @p0", "LiberacionAeronaveMinutos").FirstOrDefault();
            if (!preparacion.HasValue || !liberacion.HasValue)
            {
                ModelState.AddModelError("", "Faltan los parametros aprobados de preparacion y liberacion de aeronaves.");
                return;
            }

            var vuelosQuery = db.Vueloes
                .Include(v => v.Frecuencia.Ruta)
                .Where(v => v.AeronaveId == vuelo.AeronaveId && v.EstadoVueloCodigo != "CANCELADO");
            if (vueloActualId.HasValue)
            {
                vuelosQuery = vuelosQuery.Where(v => v.VueloId != vueloActualId.Value);
            }
            var vuelosAnteriores = vuelosQuery.ToList();
            var salida = vuelo.FechaOperacion.Date.Add(frecuencia.HoraSalida);
            var llegada = vuelo.FechaOperacion.Date.Add(frecuencia.HoraLlegada);
            var anterior = vuelosAnteriores
                .Where(v => v.FechaOperacion.Date.Add(v.Frecuencia.HoraSalida) <= salida)
                .OrderByDescending(v => v.FechaOperacion.Date.Add(v.Frecuencia.HoraSalida))
                .ThenByDescending(v => v.VueloId)
                .FirstOrDefault();
            if (anterior != null)
            {
                var llegadaAnterior = anterior.FechaOperacion.Date.Add(anterior.Frecuencia.HoraLlegada);
                var llegadaConLiberacion = llegadaAnterior.AddMinutes(liberacion.Value);
                var salidaConPreparacion = salida.AddMinutes(-preparacion.Value);
                if (anterior.Frecuencia.Ruta.AeropuertoDestinoId != frecuencia.Ruta.AeropuertoOrigenId ||
                    salidaConPreparacion < llegadaConLiberacion)
                {
                    ModelState.AddModelError("AeronaveId", "La aeronave tiene un vuelo previo incompatible o no cumple el tiempo de preparacion aprobado.");
                }
            }

            var siguiente = vuelosAnteriores
                .Where(v => v.FechaOperacion.Date.Add(v.Frecuencia.HoraSalida) > salida)
                .OrderBy(v => v.FechaOperacion.Date.Add(v.Frecuencia.HoraSalida))
                .ThenBy(v => v.VueloId)
                .FirstOrDefault();
            if (siguiente != null)
            {
                var siguienteSalida = siguiente.FechaOperacion.Date.Add(siguiente.Frecuencia.HoraSalida);
                var siguienteSalidaConPreparacion = siguienteSalida.AddMinutes(-preparacion.Value);
                var llegadaConLiberacion = llegada.AddMinutes(liberacion.Value);
                if (siguiente.Frecuencia.Ruta.AeropuertoOrigenId != frecuencia.Ruta.AeropuertoDestinoId ||
                    siguienteSalidaConPreparacion < llegadaConLiberacion)
                {
                    ModelState.AddModelError("AeronaveId", "La aeronave tiene un vuelo posterior incompatible o no cumple el tiempo de liberacion aprobado.");
                }
            }
        }

        private void ValidarEstadoVuelo(string estadoCodigo)
        {
            if (string.IsNullOrWhiteSpace(estadoCodigo) || !db.EstadoVueloes.Any(e => e.Codigo == estadoCodigo))
            {
                ModelState.AddModelError("EstadoVueloCodigo", "Seleccione un estado de vuelo valido.");
            }
        }

        private SelectList CrearOpcionesAeronave(int? seleccionada = null)
        {
            var aeronaves = db.Aeronaves
                .Include(a => a.ModeloAeronave.Asientoes)
                .Where(a => a.EstadoAeronaveCodigo == "DISPONIBLE" || a.AeronaveId == seleccionada)
                .ToList()
                .Where(a => a.ModeloAeronave.Asientoes.Count == a.ModeloAeronave.CapacidadAsientos &&
                    !a.ModeloAeronave.Asientoes.Any(s => s.NumeroAsiento > a.ModeloAeronave.CapacidadAsientos))
                .Select(a => new
                {
                    a.AeronaveId,
                    Nombre = a.Matricula + " - " + a.ModeloAeronave.Fabricante + " " + a.ModeloAeronave.Nombre +
                        " (" + a.ModeloAeronave.CapacidadAsientos + " asientos)"
                }).ToList();
            return new SelectList(aeronaves, "AeronaveId", "Nombre", seleccionada);
        }

        private SelectList CrearOpcionesFrecuencia(int? seleccionada = null)
        {
            var opciones = db.Frecuencias
                .Include(f => f.Ruta.Aeropuerto)
                .Include(f => f.Ruta.Aeropuerto1)
                .OrderBy(f => f.DiaSemana)
                .ThenBy(f => f.HoraSalida)
                .ToList()
                .Select(f => new
                {
                    f.FrecuenciaId,
                    Nombre = "F" + f.FrecuenciaId + " - " + NombreDia(f.DiaSemana) + " " +
                        f.HoraSalida.ToString(@"hh\:mm") + " - " + f.Ruta.Aeropuerto.CodigoIATA + " a " +
                        f.Ruta.Aeropuerto1.CodigoIATA + (f.Activa && f.Ruta.Activa ? "" : " (INACTIVA)")
                }).ToList();
            return new SelectList(opciones, "FrecuenciaId", "Nombre", seleccionada);
        }

        private static string NombreDia(byte dia)
        {
            switch (dia)
            {
                case 1: return "Lunes";
                case 2: return "Martes";
                case 3: return "Miercoles";
                case 4: return "Jueves";
                case 5: return "Viernes";
                case 6: return "Sabado";
                case 7: return "Domingo";
                default: return "Dia " + dia;
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
