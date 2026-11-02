using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using proU1G7_jameza1_jsremache1.Models;

namespace proU1G7_jameza1_jsremache1.Controllers
{
    // Este controlador conecta las rutas del mapa con las pantallas Razor.
    // Los pasos de selección se guardan temporalmente en Session; no se registra una reserva ficticia.
    public class InterfacesController : Controller
    {
        private readonly AerolineaVIPEntities db = new AerolineaVIPEntities();

        [AllowAnonymous]
        public ActionResult Buscar(string origen = null, string destino = null, DateTime? fecha = null, int pasajeros = 1)
        {
            var model = new InterfacesViewModel
            {
                Origen = origen,
                Destino = destino,
                FechaViaje = fecha,
                CantidadPasajeros = pasajeros >= 1 && pasajeros <= 7 ? pasajeros : 1
            };
            CargarAeropuertos(model);
            var aeropuerto = string.IsNullOrWhiteSpace(origen) ? null : BuscarAeropuerto(origen);
            CargarDestinos(model, aeropuerto == null ? (int?)null : aeropuerto.AeropuertoId);
            CargarRutasOperacion(model, aeropuerto == null ? (int?)null : aeropuerto.AeropuertoId);
            return View("Buscar", model);
        }

        [AllowAnonymous]
        public ActionResult Resultados(string origen, string destino, DateTime? fecha, int pasajeros = 1)
        {
            var model = new InterfacesViewModel
            {
                Origen = origen,
                Destino = destino,
                FechaViaje = fecha,
                CantidadPasajeros = pasajeros
            };
            Session["CantidadSolicitada"] = pasajeros;

            if (string.IsNullOrWhiteSpace(origen) || string.IsNullOrWhiteSpace(destino)
                || !fecha.HasValue || fecha.Value.Date < DateTime.Today || pasajeros < 1 || pasajeros > 7
                || string.Equals(origen, destino, StringComparison.OrdinalIgnoreCase))
            {
                model.Mensaje = "Indique un origen, destino, fecha válida y entre 1 y 7 pasajeros.";
                return View("Resultados", model);
            }

            // CAMBIO: liberar holds vencidos antes de calcular el inventario visible.
            ExpirarReservasVencidas();
            var aeropuertoOrigen = BuscarAeropuerto(origen);
            var aeropuertoDestino = BuscarAeropuerto(destino);
            if (aeropuertoOrigen == null || aeropuertoDestino == null)
            {
                model.Mensaje = "No existe una ruta para los aeropuertos seleccionados.";
                return View("Resultados", model);
            }

            var rutas = db.Rutas
                .Where(r => r.Activa && r.AeropuertoOrigenId == aeropuertoOrigen.AeropuertoId &&
                            r.AeropuertoDestinoId == aeropuertoDestino.AeropuertoId)
                .Select(r => new { r.RutaId, r.TarifaReferencial })
                .ToList();
            var rutaIds = rutas.Select(r => r.RutaId).ToList();
            var vuelos = db.Vueloes
                .Include("Frecuencia.Ruta")
                .Include("Aeronave.ModeloAeronave")
                .Include("EstadoVuelo")
                .Where(v => v.FechaOperacion == fecha.Value && rutaIds.Contains(v.Frecuencia.RutaId) &&
                            v.EstadoVueloCodigo != "CANCELADO" && v.AeronaveId != null)
                .OrderBy(v => v.Frecuencia.HoraSalida)
                .ToList();

            var ahora = DateTime.Now;
            vuelos = vuelos.Where(v => v.FechaOperacion.Date.Add(v.Frecuencia.HoraSalida) > ahora).ToList();
            foreach (var vuelo in vuelos)
            {
                var ruta = rutas.First(r => r.RutaId == vuelo.Frecuencia.RutaId);
                var asignacionesVigentes = db.ReservaPasajeroVueloes
                    .Where(a => a.VueloId == vuelo.VueloId &&
                                (a.EstadoAsignacionCodigo == "CONFIRMADA" ||
                                 (a.EstadoAsignacionCodigo == "PENDIENTE" &&
                                  a.Reserva.FechaExpiracion.HasValue && a.Reserva.FechaExpiracion > ahora)))
                    .Select(a => a.NumeroAsiento)
                    .Distinct()
                    .Count();
                var origenVuelo = db.Aeropuertoes.Find(aeropuertoOrigen.AeropuertoId);
                var destinoVuelo = db.Aeropuertoes.Find(aeropuertoDestino.AeropuertoId);
                model.Vuelos.Add(new VueloTarjetaViewModel
                {
                    VueloId = vuelo.VueloId,
                    CodigoVuelo = "VIP" + vuelo.VueloId.ToString("D5"),
                    Origen = origenVuelo.Localidad + " (" + origenVuelo.CodigoIATA + ")",
                    Destino = destinoVuelo.Localidad + " (" + destinoVuelo.CodigoIATA + ")",
                    Fecha = vuelo.FechaOperacion,
                    HoraSalida = vuelo.Frecuencia.HoraSalida.ToString(@"hh\:mm"),
                    HoraLlegada = vuelo.Frecuencia.HoraLlegada.ToString(@"hh\:mm"),
                    Matricula = vuelo.Aeronave.Matricula,
                    Estado = vuelo.EstadoVuelo.Descripcion,
                    AsientosDisponibles = Math.Max(0, vuelo.Aeronave.ModeloAeronave.CapacidadAsientos - asignacionesVigentes),
                    Tarifa = ruta.TarifaReferencial,
                    Moneda = "USD"
                });
            }

            if (model.Vuelos.Count == 0)
            {
                model.Mensaje = "No hay vuelos programados para los criterios seleccionados. No se muestran horarios inventados.";
            }

            return View("Resultados", model);
        }

        [AllowAnonymous]
        public ActionResult Disponibilidad(int? id)
        {
            if (!id.HasValue)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            ExpirarReservasVencidas();
            var vuelo = CargarVueloConAsientos(id.Value);
            if (vuelo == null)
            {
                return HttpNotFound();
            }

            ViewBag.PuedeSeleccionar = false;
            return View("Asientos", CrearModeloAsientos(vuelo, false));
        }

        [Authorize]
        public ActionResult SeleccionarAsiento(int? id)
        {
            if (!id.HasValue)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            ExpirarReservasVencidas();
            var vuelo = CargarVueloConAsientos(id.Value);
            if (vuelo == null)
            {
                return HttpNotFound();
            }

            ViewBag.PuedeSeleccionar = true;
            var model = CrearModeloAsientos(vuelo, true);
            if (TempData["Aviso"] is string)
            {
                model.Mensaje = (string)TempData["Aviso"];
            }
            return View("Asientos", model);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult SeleccionarAsiento(int id, string[] asientos)
        {
            ExpirarReservasVencidas();
            var vuelo = CargarVueloConAsientos(id);
            if (vuelo == null)
            {
                return HttpNotFound();
            }

            var modelo = CrearModeloAsientos(vuelo, true);
            ViewBag.PuedeSeleccionar = true;
            var seleccion = (asientos ?? new string[0]).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var disponibles = new HashSet<string>(
                modelo.Asientos.Where(a => a.Seleccionable).Select(a => a.Codigo),
                StringComparer.OrdinalIgnoreCase);

            var cantidadSolicitada = Session["CantidadSolicitada"] is int ? (int)Session["CantidadSolicitada"] : 1;
            if (seleccion.Count != cantidadSolicitada || seleccion.Count > vuelo.Aeronave.ModeloAeronave.CapacidadAsientos ||
                seleccion.Any(codigo => !disponibles.Contains(codigo)))
            {
                ModelState.AddModelError("", "Seleccione al menos un asiento disponible. La disponibilidad se valida nuevamente en el servidor.");
                return View("Asientos", modelo);
            }

            // Se guarda solo una selección temporal; todavía no crea reserva ni bloquea inventario.
            Session["VueloSeleccionadoId"] = vuelo.VueloId;
            Session["AsientosSeleccionados"] = seleccion;
            Session["PasajerosBorrador"] = null;
            return RedirectToRoute("ReservaPasajeros");
        }

        [Authorize]
        public ActionResult Pasajeros()
        {
            var model = CrearModeloBorrador();
            if (model == null)
            {
                TempData["Aviso"] = "Seleccione un vuelo y sus asientos antes de registrar pasajeros.";
                return RedirectToRoute("FlightSearch");
            }

            CargarTiposDocumento(model);
            return View("Pasajeros", model);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult Pasajeros(IList<PasajeroInterfazViewModel> pasajeros)
        {
            var borrador = CrearModeloBorrador();
            if (borrador == null)
            {
                return RedirectToRoute("FlightSearch");
            }

            if (pasajeros == null || pasajeros.Count != borrador.Pasajeros.Count)
            {
                ModelState.AddModelError("", "Debe ingresar los datos de cada pasajero asociado a los asientos seleccionados.");
            }
            else
            {
                var codigosDocumento = new HashSet<string>(db.TipoDocumentoIdentidads.Select(t => t.Codigo).ToList(), StringComparer.OrdinalIgnoreCase);
                for (var i = 0; i < pasajeros.Count; i++)
                {
                    if (pasajeros[i] == null)
                    {
                        pasajeros[i] = new PasajeroInterfazViewModel();
                    }
                    pasajeros[i].Numero = i + 1;
                    pasajeros[i].Asiento = borrador.Pasajeros[i].Asiento;
                    pasajeros[i].NombreCompleto = (pasajeros[i].Nombres ?? "").Trim() + " " + (pasajeros[i].Apellidos ?? "").Trim();
                    if (string.IsNullOrWhiteSpace(pasajeros[i].Nombres))
                    {
                        ModelState.AddModelError("pasajeros[" + i + "].Nombres", "Ingrese los nombres del pasajero.");
                    }
                    if (string.IsNullOrWhiteSpace(pasajeros[i].Apellidos))
                    {
                        ModelState.AddModelError("pasajeros[" + i + "].Apellidos", "Ingrese los apellidos del pasajero.");
                    }
                    if (string.IsNullOrWhiteSpace(pasajeros[i].TipoDocumento) || !codigosDocumento.Contains(pasajeros[i].TipoDocumento))
                    {
                        ModelState.AddModelError("pasajeros[" + i + "].TipoDocumento", "Seleccione un tipo de documento válido.");
                    }
                    if (string.IsNullOrWhiteSpace(pasajeros[i].Documento) || pasajeros[i].Documento.Trim().Length < 5 || pasajeros[i].Documento.Trim().Length > 20)
                    {
                        ModelState.AddModelError("pasajeros[" + i + "].Documento", "El documento debe tener entre 5 y 20 caracteres.");
                    }
                    else if (string.Equals(pasajeros[i].TipoDocumento, "CEDULA", StringComparison.OrdinalIgnoreCase)
                        && !CedulaEcuatoriana.EsValida(pasajeros[i].Documento.Trim()))
                    {
                        ModelState.AddModelError("pasajeros[" + i + "].Documento", "Ingrese una cédula ecuatoriana válida de 10 dígitos.");
                    }
                }

                if (pasajeros.Where(p => !string.IsNullOrWhiteSpace(p.Documento))
                    .GroupBy(p => p.TipoDocumento + ":" + p.Documento.Trim(), StringComparer.OrdinalIgnoreCase)
                    .Any(g => g.Count() > 1))
                {
                    ModelState.AddModelError("", "No repita el mismo documento en la operación.");
                }
            }

            if (!ModelState.IsValid)
            {
                var datos = pasajeros ?? new List<PasajeroInterfazViewModel>();
                for (var i = 0; i < borrador.Pasajeros.Count && i < datos.Count; i++)
                {
                    datos[i].Numero = i + 1;
                    datos[i].Asiento = borrador.Pasajeros[i].Asiento;
                }
                borrador.Pasajeros = datos.Count == 0 ? borrador.Pasajeros : datos;
                CargarTiposDocumento(borrador);
                return View("Pasajeros", borrador);
            }

            Session["PasajerosBorrador"] = pasajeros.ToList();
            return RedirectToRoute("ReservaConfirmar");
        }

        [Authorize]
        public ActionResult Confirmar()
        {
            var borrador = CrearModeloBorrador();
            if (borrador == null)
            {
                return RedirectToRoute("FlightSearch");
            }
            if (!DatosPasajerosCompletos(borrador))
            {
                TempData["Aviso"] = "Completa los datos de todos los pasajeros antes de confirmar.";
                return RedirectToRoute("ReservaPasajeros");
            }
            return View("~/Views/Reservas/Confirmar.cshtml", CrearModeloConfirmacion());
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult Confirmar(ReservaConfirmacionViewModel model, string continuar)
        {
            var borrador = CrearModeloBorrador();
            if (borrador == null)
            {
                return RedirectToRoute("FlightSearch");
            }
            if (!DatosPasajerosCompletos(borrador))
            {
                TempData["Aviso"] = "Completa los datos de todos los pasajeros antes de confirmar.";
                return RedirectToRoute("ReservaPasajeros");
            }

            var confirmacion = CrearModeloConfirmacion();
            if (model == null || confirmacion.Tramos.Count == 0 || confirmacion.Pasajeros.Count == 0)
            {
                TempData["Aviso"] = "Completa la selección de vuelo y pasajeros antes de continuar.";
                return RedirectToRoute("FlightSearch");
            }

            if (!model.AceptaCondiciones)
            {
                ModelState.AddModelError("AceptaCondiciones", "Debes aceptar las condiciones para continuar.");
                return View("~/Views/Reservas/Confirmar.cshtml", confirmacion);
            }

            if (!string.Equals(continuar, "pagar", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(continuar, "reservar", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError("", "Seleccione si desea guardar la reserva o continuar al pago simulado.");
                return View("~/Views/Reservas/Confirmar.cshtml", confirmacion);
            }

            string errorReserva;
            var reservaId = GuardarReservaBorrador(borrador, out errorReserva);
            if (!reservaId.HasValue)
            {
                TempData["Aviso"] = errorReserva;
                var vueloId = (int)Session["VueloSeleccionadoId"];
                return RedirectToRoute("SeatSelection", new { id = vueloId });
            }

            LimpiarBorradorReserva();
            if (string.Equals(continuar, "pagar", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Aviso"] = "Reserva guardada. Completa el pago simulado dentro del plazo indicado en pantalla.";
                return RedirectToRoute("PurchasePay", new { id = reservaId.Value });
            }

            TempData["Aviso"] = "Reserva guardada. Los asientos se mantienen hasta el vencimiento configurado o la salida del vuelo.";
            return RedirectToRoute("PurchaseConfirm", new { id = reservaId.Value });
        }

        private int? GuardarReservaBorrador(InterfacesViewModel borrador, out string error)
        {
            error = null;
            var vueloId = Session["VueloSeleccionadoId"] as int?;
            var codigosAsiento = Session["AsientosSeleccionados"] as List<string>;
            if (!vueloId.HasValue || codigosAsiento == null || borrador.Pasajeros == null ||
                codigosAsiento.Count != borrador.Pasajeros.Count || !DatosPasajerosCompletos(borrador))
            {
                error = "Los datos de la reserva ya no estan completos. Inicia nuevamente la seleccion.";
                return null;
            }

            var ahora = DateTime.Now;
            using (var transaction = db.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                // Bloquea la fila del vuelo para serializar reservas concurrentes del mismo vuelo.
                db.Database.ExecuteSqlCommand(
                    "SELECT * FROM dbo.Vuelo WITH (UPDLOCK, HOLDLOCK) WHERE VueloId = @p0",
                    vueloId.Value);

                var vuelo = db.Vueloes
                    .AsNoTracking()
                    .Include("Frecuencia.Ruta")
                    .Include("Aeronave.ModeloAeronave.Asientoes")
                    .FirstOrDefault(v => v.VueloId == vueloId.Value);
                var salidaVuelo = vuelo == null ? DateTime.MinValue : vuelo.FechaOperacion.Date.Add(vuelo.Frecuencia.HoraSalida);
                if (vuelo == null || vuelo.Aeronave == null || salidaVuelo <= ahora ||
                    vuelo.EstadoVueloCodigo == "CANCELADO" || !vuelo.Frecuencia.Ruta.Activa)
                {
                    error = "El vuelo ya no esta disponible para reservar. Busca otro vuelo.";
                    return null;
                }

                if (!db.EstadoReservas.Any(e => e.Codigo == "PENDIENTE") ||
                    !db.EstadoAsignacions.Any(e => e.Codigo == "PENDIENTE"))
                {
                    throw new InvalidOperationException("Falta configurar el estado PENDIENTE para guardar reservas y asignaciones.");
                }

                var ocupados = new HashSet<byte>(db.ReservaPasajeroVueloes
                    .Where(a => a.VueloId == vuelo.VueloId &&
                                (a.EstadoAsignacionCodigo == "CONFIRMADA" ||
                                 (a.EstadoAsignacionCodigo == "PENDIENTE" &&
                                  a.Reserva.FechaExpiracion.HasValue && a.Reserva.FechaExpiracion > ahora)))
                    .Select(a => a.NumeroAsiento)
                    .ToList());
                var asientosDisponibles = vuelo.Aeronave.ModeloAeronave.Asientoes
                    .Where(a => !ocupados.Contains(a.NumeroAsiento))
                    .ToDictionary(a => a.Codigo, StringComparer.OrdinalIgnoreCase);
                if (codigosAsiento.Distinct(StringComparer.OrdinalIgnoreCase).Count() != codigosAsiento.Count ||
                    codigosAsiento.Any(codigo => !asientosDisponibles.ContainsKey(codigo)))
                {
                    error = "Uno o mas asientos ya no estan disponibles. Selecciona nuevamente los asientos.";
                    return null;
                }

                var numerosAsientoSolicitados = codigosAsiento
                    .Select(codigo => asientosDisponibles[codigo].NumeroAsiento)
                    .Distinct()
                    .ToList();
                if (numerosAsientoSolicitados.Count != codigosAsiento.Count)
                {
                    error = "Uno o mas asientos ya no estan disponibles. Selecciona nuevamente los asientos.";
                    return null;
                }

                BloquearAsientosEnVuelo(vuelo.VueloId, numerosAsientoSolicitados);

                var ocupadosTrasBloqueo = new HashSet<byte>(db.ReservaPasajeroVueloes
                    .Where(a => a.VueloId == vuelo.VueloId && numerosAsientoSolicitados.Contains(a.NumeroAsiento) &&
                                (a.EstadoAsignacionCodigo == "CONFIRMADA" ||
                                 (a.EstadoAsignacionCodigo == "PENDIENTE" &&
                                  a.Reserva.FechaExpiracion.HasValue && a.Reserva.FechaExpiracion > ahora)))
                    .Select(a => a.NumeroAsiento)
                    .ToList());
                if (ocupadosTrasBloqueo.Count > 0)
                {
                    error = "Uno o mas asientos ya no estan disponibles. Selecciona nuevamente los asientos.";
                    return null;
                }

                LiberarAsignacionesVencidas(vuelo.VueloId, numerosAsientoSolicitados, ahora);

                var reserva = new Reserva
                {
                    UsuarioId = User.Identity.GetUserId(),
                    FechaCreacion = ahora,
                    FechaExpiracion = salidaVuelo < ahora.AddMinutes(LeerParametroPositivo("ReservaRetencionMinutos"))
                        ? salidaVuelo
                        : ahora.AddMinutes(LeerParametroPositivo("ReservaRetencionMinutos")),
                    EstadoReservaCodigo = "PENDIENTE"
                };
                db.Reservas.Add(reserva);

                for (var i = 0; i < borrador.Pasajeros.Count; i++)
                {
                    var datos = borrador.Pasajeros[i];
                    var documento = datos.Documento.Trim();
                    var pasajero = db.Pasajeroes.FirstOrDefault(p =>
                        p.TipoDocumentoCodigo == datos.TipoDocumento && p.NumeroDocumento == documento);
                    if (pasajero == null)
                    {
                        pasajero = new Pasajero
                        {
                            TipoDocumentoCodigo = datos.TipoDocumento,
                            NumeroDocumento = documento,
                            Nombres = datos.Nombres.Trim(),
                            Apellidos = datos.Apellidos.Trim(),
                            FechaRegistro = ahora
                        };
                        db.Pasajeroes.Add(pasajero);
                    }
                    else
                    {
                        pasajero.Nombres = datos.Nombres.Trim();
                        pasajero.Apellidos = datos.Apellidos.Trim();
                    }

                    var asiento = asientosDisponibles[codigosAsiento[i]];
                    db.ReservaPasajeroVueloes.Add(new ReservaPasajeroVuelo
                    {
                        Reserva = reserva,
                        Pasajero = pasajero,
                        VueloId = vuelo.VueloId,
                        NumeroAsiento = asiento.NumeroAsiento,
                        ImporteTramo = vuelo.Frecuencia.Ruta.TarifaReferencial,
                        EstadoAsignacionCodigo = "PENDIENTE",
                        FechaAsignacion = ahora
                    });
                }

                try
                {
                    db.SaveChanges();
                }
                catch (DbUpdateException ex) when (EsDuplicadoAsiento(ex))
                {
                    transaction.Rollback();
                    error = "Uno o mas asientos ya no estan disponibles. Selecciona nuevamente los asientos.";
                    return null;
                }

                var reservaId = reserva.ReservaId;
                transaction.Commit();
                return reservaId;
            }
        }

        // CAMBIO: aplica DP-07 al consultar y antes de reservar; los estados ya existen en el script SQL.
        private void ExpirarReservasVencidas()
        {
            var ahora = DateTime.Now;
            using (var transaction = db.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                var vencidas = db.Reservas
                    .Include("ReservaPasajeroVueloes")
                    .Where(r => r.EstadoReservaCodigo == "PENDIENTE" &&
                                (!r.FechaExpiracion.HasValue || r.FechaExpiracion <= ahora))
                    .ToList();
                if (vencidas.Count == 0)
                {
                    transaction.Commit();
                    return;
                }

                if (!db.EstadoReservas.Any(e => e.Codigo == "EXPIRADA") ||
                    !db.EstadoAsignacions.Any(e => e.Codigo == "LIBERADA"))
                {
                    throw new InvalidOperationException("Falta configurar EXPIRADA y LIBERADA en los catálogos de reservas.");
                }

                foreach (var reserva in vencidas)
                {
                    reserva.EstadoReservaCodigo = "EXPIRADA";
                    foreach (var asignacion in reserva.ReservaPasajeroVueloes.Where(a => a.EstadoAsignacionCodigo == "PENDIENTE"))
                    {
                        asignacion.EstadoAsignacionCodigo = "LIBERADA";
                    }
                }

                db.SaveChanges();
                transaction.Commit();
            }
        }

        private void LiberarAsignacionesVencidas(int vueloId, IEnumerable<byte> numerosAsiento, DateTime ahora)
        {
            var numeros = numerosAsiento.Distinct().ToList();
            var vencidas = db.ReservaPasajeroVueloes
                .Include("Reserva")
                .Where(a => a.VueloId == vueloId && numeros.Contains(a.NumeroAsiento) &&
                            a.EstadoAsignacionCodigo == "PENDIENTE" &&
                            (!a.Reserva.FechaExpiracion.HasValue || a.Reserva.FechaExpiracion <= ahora))
                .ToList();
            if (vencidas.Count == 0)
            {
                return;
            }

            foreach (var asignacion in vencidas)
            {
                asignacion.EstadoAsignacionCodigo = "LIBERADA";
                asignacion.Reserva.EstadoReservaCodigo = "EXPIRADA";
            }

            db.SaveChanges();
        }

        private void BloquearAsientosEnVuelo(int vueloId, IEnumerable<byte> numerosAsiento)
        {
            var numeros = numerosAsiento.Distinct().ToList();
            if (!numeros.Any())
            {
                return;
            }

            var sql = new StringBuilder();
            sql.Append("SELECT a.AsignacionId FROM dbo.ReservaPasajeroVuelo AS a WITH (UPDLOCK, HOLDLOCK) WHERE a.VueloId = @p0 AND a.NumeroAsiento IN (");

            var parameters = new object[numeros.Count + 1];
            parameters[0] = vueloId;
            for (var i = 0; i < numeros.Count; i++)
            {
                if (i > 0)
                {
                    sql.Append(", ");
                }

                sql.Append("@p");
                sql.Append(i + 1);
                parameters[i + 1] = numeros[i];
            }

            sql.Append(")");
            db.Database.ExecuteSqlCommand(sql.ToString(), parameters);
        }

        private static bool EsDuplicadoAsiento(DbUpdateException exception)
        {
            for (Exception ex = exception; ex != null; ex = ex.InnerException)
            {
                var message = ex.Message ?? string.Empty;
                if ((message.IndexOf("duplicate key", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     message.IndexOf("Violation of UNIQUE KEY constraint", StringComparison.OrdinalIgnoreCase) >= 0) &&
                    (message.IndexOf("ReservaPasajeroVuelo", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     message.IndexOf("UX_RPV_AsientoActivo", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     message.IndexOf("NumeroAsiento", StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    return true;
                }

                var sqlException = ex as SqlException;
                if (sqlException != null &&
                    (message.IndexOf("ReservaPasajeroVuelo", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     message.IndexOf("UX_RPV_AsientoActivo", StringComparison.OrdinalIgnoreCase) >= 0) &&
                    sqlException.Errors.Cast<SqlError>().Any(error => error.Number == 2601 || error.Number == 2627))
                {
                    return true;
                }
            }

            return false;
        }

        private void LimpiarBorradorReserva()
        {
            Session.Remove("VueloSeleccionadoId");
            Session.Remove("AsientosSeleccionados");
            Session.Remove("PasajerosBorrador");
            Session.Remove("CantidadSolicitada");
        }

        private static int LeerParametroPositivo(string clave)
        {
            int valor;
            if (!int.TryParse(ConfigurationManager.AppSettings[clave], NumberStyles.None, CultureInfo.InvariantCulture, out valor) || valor <= 0)
            {
                throw new InvalidOperationException("El parámetro " + clave + " debe ser un entero positivo en Web.config.");
            }

            return valor;
        }

        private static bool ReservaPendienteVencida(Reserva reserva, DateTime ahora)
        {
            return reserva.EstadoReservaCodigo == "PENDIENTE" &&
                (!reserva.FechaExpiracion.HasValue || reserva.FechaExpiracion.Value <= ahora);
        }

        [Authorize]
        public ActionResult Pagar(int? id)
        {
            ExpirarReservasVencidas();
            if (!id.HasValue || id.Value <= 0)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var usuarioId = User.Identity.GetUserId();
            var esCounter = User.IsInRole("Counter");
            var reserva = db.Reservas
                .Include("EstadoReserva")
                .Include("Compras.EstadoCompra")
                .Include("Compras.Pagoes.EstadoPago")
                .Include("ReservaPasajeroVueloes.Pasajero")
                .Include("ReservaPasajeroVueloes.Vuelo.Frecuencia.Ruta")
                .FirstOrDefault(r => r.ReservaId == id.Value && (r.UsuarioId == usuarioId || esCounter));
            if (reserva == null)
            {
                return HttpNotFound();
            }
            if (reserva.EstadoReservaCodigo != "PENDIENTE")
            {
                if (reserva.EstadoReservaCodigo == "CONFIRMADA" &&
                    EmitirComprobanteParaPagoSimulado(reserva.ReservaId))
                {
                    TempData["Aviso"] = "El pago simulado ya estaba registrado. Revisa el comprobante emitido.";
                    return RedirectToRoute("Receipts");
                }

                TempData["Aviso"] = "Solo se puede pagar una reserva pendiente.";
                return RedirectToRoute("PurchaseConfirm", new { id = reserva.ReservaId });
            }
            if (ReservaPendienteVencida(reserva, DateTime.Now))
            {
                TempData["Aviso"] = "La reserva vencio y sus asientos ya no estan retenidos. Selecciona nuevamente el vuelo.";
                return RedirectToRoute("MyReservations");
            }

            var model = CrearModeloPago(reserva);
            return View("~/Views/Compras/Pagar.cshtml", model);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult Pagar(int id, PagoSimuladoViewModel model)
        {
            if (id <= 0)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // CAMBIO: registrar vencimientos antes de validar el cobro evita pagar inventario liberado.
            ExpirarReservasVencidas();
            model = model ?? new PagoSimuladoViewModel();
            var reserva = ObtenerReservaPropia(id);
            if (reserva == null)
            {
                return HttpNotFound();
            }
            if (reserva.EstadoReservaCodigo != "PENDIENTE")
            {
                if (reserva.EstadoReservaCodigo == "CONFIRMADA" &&
                    EmitirComprobanteParaPagoSimulado(reserva.ReservaId))
                {
                    TempData["Aviso"] = "El pago simulado ya estaba registrado. Revisa el comprobante emitido.";
                    return RedirectToRoute("Receipts");
                }

                TempData["Aviso"] = "Solo se puede pagar una reserva pendiente.";
                return RedirectToRoute("PurchaseConfirm", new { id = reserva.ReservaId });
            }
            if (ReservaPendienteVencida(reserva, DateTime.Now))
            {
                TempData["Aviso"] = "La reserva vencio y sus asientos ya no estan retenidos. Selecciona nuevamente el vuelo.";
                return RedirectToRoute("MyReservations");
            }

            var modeloServidor = CrearModeloPago(reserva);
            if (modeloServidor == null)
            {
                TempData["Aviso"] = "La reserva ya no esta disponible para pagar. Inicia nuevamente la busqueda.";
                return RedirectToRoute("MyReservations");
            }

            var marca = db.MarcaTarjetas.FirstOrDefault(m => m.Activa && m.Codigo == model.MarcaTarjetaCodigo);
            var metodo = db.MetodoPagoes.FirstOrDefault(m => m.Activo && m.Codigo == model.MetodoPagoCodigo);
            var numero = (model.NumeroTarjeta ?? string.Empty).Replace(" ", string.Empty);
            var nombreTitular = model.NombreTitular ?? string.Empty;
            if (nombreTitular.Any(caracter => !char.IsLetter(caracter) && caracter != ' '))
            {
                ModelState.AddModelError("NombreTitular", "Use solo letras y espacios en el nombre del titular.");
            }
            if (marca == null)
            {
                ModelState.AddModelError("MarcaTarjetaCodigo", "Seleccione una marca activa.");
            }
            else
            {
                if (numero.Length != marca.LongitudNumero || !numero.All(char.IsDigit))
                {
                    ModelState.AddModelError("NumeroTarjeta", "El número no coincide con la longitud configurada para la marca seleccionada.");
                }
                if (string.IsNullOrEmpty(model.CodigoSeguridad) || model.CodigoSeguridad.Length != marca.LongitudCodigoSeguridad || !model.CodigoSeguridad.All(char.IsDigit))
                {
                    ModelState.AddModelError("CodigoSeguridad", "El código de seguridad no coincide con la longitud configurada para la marca seleccionada.");
                }
            }
            DateTime vencimiento;
            if (!DateTime.TryParseExact(model.Vencimiento, "MM/yy", CultureInfo.InvariantCulture, DateTimeStyles.None, out vencimiento)
                || vencimiento < new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1))
            {
                ModelState.AddModelError("Vencimiento", "La tarjeta debe tener una fecha válida y no vencida.");
            }
            if (metodo == null)
            {
                ModelState.AddModelError("MetodoPagoCodigo", "Seleccione un método de pago activo.");
            }

            model.ReservaId = modeloServidor.ReservaId;
            model.CodigoReserva = modeloServidor.CodigoReserva;
            model.Ruta = modeloServidor.Ruta;
            model.FechaViaje = modeloServidor.FechaViaje;
            model.Horario = modeloServidor.Horario;
            model.CantidadPasajeros = modeloServidor.CantidadPasajeros;
            model.Asientos = modeloServidor.Asientos;
            model.Total = modeloServidor.Total;
            model.MonedaCodigo = modeloServidor.MonedaCodigo;
            model.EstadoCompra = modeloServidor.EstadoCompra;
            CargarOpcionesPago(model);

            if (!ModelState.IsValid)
            {
                model.NumeroTarjeta = null;
                model.CodigoSeguridad = null;
                ModelState.SetModelValue("NumeroTarjeta", new ValueProviderResult(null, string.Empty, CultureInfo.CurrentCulture));
                ModelState.SetModelValue("CodigoSeguridad", new ValueProviderResult(null, string.Empty, CultureInfo.CurrentCulture));
                return View("~/Views/Compras/Pagar.cshtml", model);
            }

            var pagoAprobado = !numero.EndsWith("0000", StringComparison.Ordinal);
            using (var transaction = db.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                var compraExistente = db.Compras.FirstOrDefault(c => c.ReservaId == reserva.ReservaId &&
                    (c.EstadoCompraCodigo == "PENDIENTE" || c.EstadoCompraCodigo == "PAGADA"));
                if (compraExistente != null && compraExistente.EstadoCompraCodigo == "PAGADA")
                {
                    EmitirComprobanteSimulado(compraExistente, DateTime.Now);
                    transaction.Commit();
                    TempData["Aviso"] = "Esta compra ya estaba pagada. Revisa el comprobante emitido.";
                    return RedirectToRoute("Receipts");
                }

                db.Entry(reserva).Reload();
                var ahora = DateTime.Now;
                if (reserva.EstadoReservaCodigo != "PENDIENTE" || ReservaPendienteVencida(reserva, ahora))
                {
                    transaction.Commit();
                    TempData["Aviso"] = "La reserva ya no está pendiente o venció. Revisa tus reservas.";
                    return RedirectToRoute("MyReservations");
                }

                if (!db.EstadoReservas.Any(e => e.Codigo == "CONFIRMADA") ||
                    !db.EstadoAsignacions.Any(e => e.Codigo == "CONFIRMADA") ||
                    !db.EstadoCompras.Any(e => e.Codigo == (pagoAprobado ? "PAGADA" : "PENDIENTE")) ||
                    !db.EstadoPagoes.Any(e => e.Codigo == (pagoAprobado ? "APROBADO" : "RECHAZADO")) ||
                    !db.Monedas.Any(m => m.Codigo == "USD"))
                {
                    throw new InvalidOperationException("Faltan estados comerciales o la moneda USD en los catálogos de AerolineaVIP.");
                }

                var compra = compraExistente ?? new Compra
                {
                    ReservaId = reserva.ReservaId,
                    FechaCompra = ahora,
                    Importe = modeloServidor.Total,
                    MonedaCodigo = "USD",
                    EstadoCompraCodigo = "PENDIENTE",
                    ClaveIdempotencia = Guid.NewGuid()
                };
                if (compraExistente == null)
                {
                    db.Compras.Add(compra);
                    db.SaveChanges();
                }

                db.Pagoes.Add(new Pago
                {
                    CompraId = compra.CompraId,
                    FechaPago = ahora,
                    Importe = modeloServidor.Total,
                    EstadoPagoCodigo = pagoAprobado ? "APROBADO" : "RECHAZADO",
                    MetodoPagoCodigo = metodo.Codigo,
                    MarcaTarjetaCodigo = marca.Codigo,
                    UltimosCuatroDigitos = numero.Substring(numero.Length - 4),
                    NombreTitular = nombreTitular.Trim(),
                    CodigoOperacion = "SIM-" + Guid.NewGuid().ToString("N").Substring(0, 12).ToUpperInvariant(),
                    EsSimulado = true,
                    MensajeResultado = pagoAprobado
                        ? "Pago de demostración aprobado; no se realizó ningún cobro."
                        : "Pago de demostración rechazado por la regla de prueba DP-06."
                });

                if (pagoAprobado)
                {
                    compra.EstadoCompraCodigo = "PAGADA";
                    reserva.EstadoReservaCodigo = "CONFIRMADA";
                    reserva.FechaExpiracion = null;
                    foreach (var asignacion in db.ReservaPasajeroVueloes.Where(a => a.ReservaId == reserva.ReservaId))
                    {
                        asignacion.EstadoAsignacionCodigo = "CONFIRMADA";
                    }

                    EmitirComprobanteSimulado(compra, ahora);
                }

                db.SaveChanges();
                transaction.Commit();
            }

            if (!pagoAprobado)
            {
                TempData["Aviso"] = "Pago rechazado por la simulación. La reserva sigue pendiente; prueba con una tarjeta de datos ficticios que no termine en 0000.";
                return RedirectToRoute("PurchasePay", new { id = reserva.ReservaId });
            }

            TempData["Aviso"] = "Pago simulado confirmado y comprobante académico emitido. No se realizó ningún cobro.";
            return RedirectToRoute("Receipts");
        }

        [Authorize]
        public ActionResult Confirmacion(int? id)
        {
            if (!id.HasValue)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            if (id.Value <= 0)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var reserva = ObtenerReservaPropia(id.Value);
            if (reserva == null)
            {
                return HttpNotFound();
            }

            return View("Confirmacion", CrearModeloReserva(reserva));
        }

        [Authorize]
        public ActionResult MisReservas(string estado = null)
        {
            ExpirarReservasVencidas();
            var usuarioId = User.Identity.GetUserId();
            var puedeConsultarTodas = User.IsInRole("Counter") || User.IsInRole("view");
            var reservas = db.Reservas
                .Include("EstadoReserva")
                .Include("Compras.EstadoCompra")
                .Include("ReservaPasajeroVueloes.Vuelo.Frecuencia.Ruta")
                .Where(r => r.UsuarioId == usuarioId || puedeConsultarTodas)
                .OrderByDescending(r => r.FechaCreacion)
                .ToList();

            var model = new InterfacesViewModel();
            foreach (var reserva in reservas)
            {
                if (!string.IsNullOrWhiteSpace(estado) && reserva.EstadoReservaCodigo != estado)
                {
                    continue;
                }
                model.Reservas.Add(CrearModeloReserva(reserva));
            }

            return View("MisReservas", model);
        }

        [Authorize]
        public ActionResult Gestionar(int? id)
        {
            ExpirarReservasVencidas();
            if (!id.HasValue)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var reserva = ObtenerReservaPropia(id.Value);
            if (reserva == null)
            {
                return HttpNotFound();
            }

            return View("Gestionar", CrearModeloReserva(reserva));
        }

        // CAMBIO: cancelar reservas sin pago y registrar solicitudes de cancelación pagadas sin ejecutar reembolsos.
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult SolicitarCancelacion(int id)
        {
            ExpirarReservasVencidas();
            var reserva = ObtenerReservaPropia(id);
            if (reserva == null)
            {
                return HttpNotFound();
            }

            using (var transaction = db.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                db.Database.ExecuteSqlCommand(
                    "SELECT ReservaId FROM dbo.Reserva WITH (UPDLOCK, HOLDLOCK) WHERE ReservaId = @p0",
                    reserva.ReservaId);
                db.Entry(reserva).Reload();
                if (reserva.EstadoReservaCodigo == "PENDIENTE" && !ReservaPendienteVencida(reserva, DateTime.Now))
                {
                    if (!db.EstadoReservas.Any(e => e.Codigo == "CANCELADA") ||
                        !db.EstadoAsignacions.Any(e => e.Codigo == "CANCELADA") ||
                        !db.EstadoCompras.Any(e => e.Codigo == "ANULADA"))
                    {
                        throw new InvalidOperationException("Faltan estados de cancelación en los catálogos de AerolineaVIP.");
                    }

                    reserva.EstadoReservaCodigo = "CANCELADA";
                    reserva.FechaExpiracion = null;
                    foreach (var asignacion in db.ReservaPasajeroVueloes.Where(a => a.ReservaId == reserva.ReservaId && a.EstadoAsignacionCodigo == "PENDIENTE"))
                    {
                        asignacion.EstadoAsignacionCodigo = "CANCELADA";
                    }
                    foreach (var compra in db.Compras.Where(c => c.ReservaId == reserva.ReservaId && c.EstadoCompraCodigo == "PENDIENTE"))
                    {
                        compra.EstadoCompraCodigo = "ANULADA";
                    }

                    db.SaveChanges();
                    transaction.Commit();
                    TempData["Aviso"] = "Reserva pendiente cancelada; los asientos quedaron disponibles.";
                }
                else if (reserva.EstadoReservaCodigo == "CONFIRMADA")
                {
                    if (!db.EstadoReservas.Any(e => e.Codigo == "SOLICITUD_CANCELACION"))
                    {
                        throw new InvalidOperationException("Ejecute la migración de solicitud de cancelación en la base AerolineaVIP.");
                    }

                    reserva.EstadoReservaCodigo = "SOLICITUD_CANCELACION";
                    db.SaveChanges();
                    transaction.Commit();
                    TempData["Aviso"] = "Solicitud registrada. La política DP-08 está pendiente de aprobación; no se anuló la compra, no se liberaron asientos ni se ejecutó reembolso.";
                }
                else
                {
                    transaction.Commit();
                    TempData["Aviso"] = "La reserva ya no permite solicitar otra cancelación.";
                }
            }

            return RedirectToRoute("ManageReservation", new { id = reserva.ReservaId });
        }

        [Authorize]
        public ActionResult CheckIn()
        {
            ExpirarReservasVencidas();
            var usuarioId = User.Identity.GetUserId();
            var model = new InterfacesViewModel { Mensaje = TempData["Aviso"] as string };
            var reservas = db.Reservas
                .Include("EstadoReserva")
                .Include("Compras.EstadoCompra")
                .Include("ReservaPasajeroVueloes.Vuelo.Frecuencia.Ruta")
                .Where(r => r.UsuarioId == usuarioId &&
                    (r.EstadoReservaCodigo == "CONFIRMADA" || r.EstadoReservaCodigo == "SOLICITUD_CANCELACION"))
                .OrderByDescending(r => r.FechaCreacion)
                .ToList();
            foreach (var reserva in reservas)
            {
                model.Reservas.Add(CrearModeloReserva(reserva));
            }
            return View("CheckIn", model);
        }

        // CAMBIO: aplica DP-12 validando titularidad, pago aprobado, ventana horaria y duplicados en servidor.
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult RegistrarCheckIn(string codigoReserva, string documento)
        {
            ExpirarReservasVencidas();
            var codigo = (codigoReserva ?? string.Empty).Trim();
            var numeroDocumento = (documento ?? string.Empty).Trim();
            if (codigo.Length == 0 || numeroDocumento.Length < 5 || numeroDocumento.Length > 20)
            {
                TempData["Aviso"] = "Ingrese el código de reserva y un documento válido.";
                return RedirectToAction("CheckIn");
            }

            var usuarioId = User.Identity.GetUserId();
            var esCounter = User.IsInRole("Counter");
            var reserva = db.Reservas
                .Include("Compras.Pagoes")
                .Include("ReservaPasajeroVueloes.Pasajero")
                .Include("ReservaPasajeroVueloes.Vuelo.Frecuencia")
                .FirstOrDefault(r => r.CodigoReserva == codigo &&
                    (r.UsuarioId == usuarioId || esCounter));
            if (reserva == null ||
                (reserva.EstadoReservaCodigo != "CONFIRMADA" && reserva.EstadoReservaCodigo != "SOLICITUD_CANCELACION") ||
                !reserva.Compras.Any(c => c.EstadoCompraCodigo == "PAGADA" && c.Pagoes.Any(p => p.EstadoPagoCodigo == "APROBADO")))
            {
                TempData["Aviso"] = "No se encontró una reserva confirmada y pagada para esos datos.";
                return RedirectToAction("CheckIn");
            }

            var asignacionesPasajero = reserva.ReservaPasajeroVueloes
                .Where(a => a.EstadoAsignacionCodigo == "CONFIRMADA" &&
                    string.Equals(a.Pasajero.NumeroDocumento, numeroDocumento, StringComparison.OrdinalIgnoreCase))
                .ToList();
            var primerTramo = reserva.ReservaPasajeroVueloes
                .OrderBy(a => a.Vuelo.FechaOperacion)
                .ThenBy(a => a.Vuelo.Frecuencia.HoraSalida)
                .FirstOrDefault();
            if (asignacionesPasajero.Count == 0 || primerTramo == null)
            {
                TempData["Aviso"] = "El documento no corresponde a un pasajero confirmado de esa reserva.";
                return RedirectToAction("CheckIn");
            }

            var salidaInicial = primerTramo.Vuelo.FechaOperacion.Date.Add(primerTramo.Vuelo.Frecuencia.HoraSalida);
            var abre = salidaInicial.AddHours(-LeerParametroPositivo("CheckInAperturaHoras"));
            var cierra = salidaInicial.AddMinutes(-LeerParametroPositivo("CheckInCierreMinutos"));
            var ahora = DateTime.Now;
            if (ahora < abre || ahora >= cierra)
            {
                TempData["Aviso"] = "El check-in abre " + abre.ToString("dd/MM/yyyy HH:mm") +
                    " y cierra " + cierra.ToString("dd/MM/yyyy HH:mm") + ".";
                return RedirectToAction("CheckIn");
            }

            using (var transaction = db.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                db.Database.ExecuteSqlCommand(
                    "SELECT ReservaId FROM dbo.Reserva WITH (UPDLOCK, HOLDLOCK) WHERE ReservaId = @p0",
                    reserva.ReservaId);
                var estadoActual = db.Reservas.Where(r => r.ReservaId == reserva.ReservaId)
                    .Select(r => r.EstadoReservaCodigo).FirstOrDefault();
                if (estadoActual != "CONFIRMADA" && estadoActual != "SOLICITUD_CANCELACION")
                {
                    transaction.Commit();
                    TempData["Aviso"] = "La reserva cambió de estado y ya no permite el check-in.";
                    return RedirectToAction("CheckIn");
                }

                if (!db.EstadoCheckIns.Any(e => e.Codigo == "COMPLETADO"))
                {
                    throw new InvalidOperationException("Falta configurar el estado COMPLETADO para check-in.");
                }

                var idsAsignacion = asignacionesPasajero.Select(a => a.AsignacionId).ToList();
                var existentes = new HashSet<int>(db.CheckIns.AsNoTracking()
                    .Where(c => idsAsignacion.Contains(c.AsignacionId) && c.EstadoCheckInCodigo == "COMPLETADO")
                    .Select(c => c.AsignacionId)
                    .ToList());
                foreach (var asignacion in asignacionesPasajero.Where(a => !existentes.Contains(a.AsignacionId)))
                {
                    db.CheckIns.Add(new CheckIn
                    {
                        AsignacionId = asignacion.AsignacionId,
                        FechaRegistro = ahora,
                        EstadoCheckInCodigo = "COMPLETADO"
                    });
                }
                db.SaveChanges();
                transaction.Commit();
            }

            TempData["Aviso"] = "Check-in registrado para los tramos confirmados de " +
                asignacionesPasajero[0].Pasajero.Nombres + " " + asignacionesPasajero[0].Pasajero.Apellidos + ".";
            return RedirectToAction("CheckIn");
        }

        private bool EmitirComprobanteParaPagoSimulado(int reservaId)
        {
            using (var transaction = db.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                var compra = db.Compras.FirstOrDefault(c => c.ReservaId == reservaId && c.EstadoCompraCodigo == "PAGADA");
                if (compra == null || !db.Pagoes.Any(p => p.CompraId == compra.CompraId && p.EsSimulado && p.EstadoPagoCodigo == "APROBADO"))
                {
                    transaction.Commit();
                    return false;
                }

                EmitirComprobanteSimulado(compra, DateTime.Now);
                transaction.Commit();
                return true;
            }
        }

        private string EmitirComprobanteSimulado(Compra compra, DateTime fechaEmision)
        {
            var comprobanteExistente = db.Comprobantes.FirstOrDefault(c => c.CompraId == compra.CompraId);
            if (comprobanteExistente != null)
            {
                return comprobanteExistente.NumeroComprobante;
            }

            if (!db.TipoComprobantes.Any(t => t.Codigo == "SIMULADO"))
            {
                db.TipoComprobantes.Add(new TipoComprobante
                {
                    Codigo = "SIMULADO",
                    Descripcion = "Comprobante ficticio de pago"
                });
            }
            if (!db.EstadoComprobantes.Any(e => e.Codigo == "EMITIDO"))
            {
                db.EstadoComprobantes.Add(new EstadoComprobante
                {
                    Codigo = "EMITIDO",
                    Descripcion = "Emitido (simulado)"
                });
            }

            db.SaveChanges();
            var comprobante = new Comprobante
            {
                CompraId = compra.CompraId,
                TipoComprobanteCodigo = "SIMULADO",
                NumeroComprobante = "SIM-" + compra.CompraId.ToString("D10"),
                FechaEmision = fechaEmision,
                EstadoComprobanteCodigo = "EMITIDO"
            };
            db.Comprobantes.Add(comprobante);
            db.SaveChanges();
            return comprobante.NumeroComprobante;
        }

        [Authorize]
        public ActionResult Comprobantes()
        {
            var usuarioId = User.Identity.GetUserId();
            var comprobantes = db.Comprobantes
                .Include("Compra.Reserva")
                .Include("EstadoComprobante")
                .Where(c => c.Compra.Reserva.UsuarioId == usuarioId)
                .OrderByDescending(c => c.FechaEmision)
                .ToList();

            var model = new InterfacesViewModel();
            foreach (var comprobante in comprobantes)
            {
                model.Tramos.Add(new TramoInterfazViewModel
                {
                    Orden = comprobante.ComprobanteId,
                    CodigoVuelo = comprobante.NumeroComprobante,
                    Ruta = comprobante.TipoComprobanteCodigo,
                    Fecha = comprobante.FechaEmision,
                    Horario = comprobante.EstadoComprobante.Descripcion
                });
            }

            return View("Comprobantes", model);
        }

        [Authorize(Roles = "view")]
        public ActionResult AdminIndex()
        {
            var model = ObtenerResumenAdministrativo();
            return View("AdminIndex", model);
        }

        [Authorize(Roles = "view")]
        public ActionResult AdminVuelos()
        {
            ViewBag.TitleModulo = "Vuelos, rutas y frecuencias";
            ViewBag.NumeroPantalla = 18;
            var model = ObtenerResumenAdministrativo();
            foreach (var acceso in new[]
            {
                Acceso("Vuelos programados", "Consultar o editar vuelos existentes.", "Vuelo", "Index"),
                Acceso("Frecuencias", "Mantener días y horarios de operación.", "Frecuencia", "Index"),
                Acceso("Rutas", "Administrar origen, destino y tarifa referencial.", "Rutas", "Index"),
                Acceso("Itinerarios", "Mantener itinerarios y sus tramos.", "Itinerario", "Index"),
                Acceso("Aeropuertos", "Configurar aeropuertos activos.", "Aeropuerto", "Index")
            })
            {
                model.AccesosAdministrativos.Add(acceso);
            }
            return View("AdminModulo", model);
        }

        [Authorize(Roles = "view")]
        public ActionResult AdminAeronaves()
        {
            ViewBag.TitleModulo = "Flota y asientos";
            ViewBag.NumeroPantalla = 19;
            var model = ObtenerResumenAdministrativo();
            foreach (var acceso in new[]
            {
                Acceso("Aeronaves", "Consultar matrículas y estado de la flota.", "Aeronaves", "Index"),
                Acceso("Modelos de aeronave", "Revisar fabricante y capacidad.", "ModeloAeronave", "Index"),
                Acceso("Asientos", "Consultar la configuración de los siete asientos.", "Asiento", "Index")
            })
            {
                model.AccesosAdministrativos.Add(acceso);
            }
            return View("AdminModulo", model);
        }

        [Authorize(Roles = "view")]
        public ActionResult AdminGestion()
        {
            ViewBag.TitleModulo = "Gestión operativa";
            ViewBag.NumeroPantalla = 20;
            var model = ObtenerResumenAdministrativo();
            foreach (var acceso in new[]
            {
                Acceso("Pasajeros", "Buscar, consultar y corregir los datos vinculados a reservas existentes.", "Interfaces", "AdminPasajeros"),
                Acceso("Reservas", "Consultar reservas persistidas y gestionar solicitudes autorizadas.", "Interfaces", "MisReservas"),
                Acceso("Tipos de documento", "Administrar el catálogo habilitado.", "TipoDocumentoIdentidads", "Index"),
                Acceso("Usuarios", "SuperUsuario puede asignar roles operativos Cliente y Counter.", User.IsInRole("SuperUsuario") ? "Interfaces" : null, User.IsInRole("SuperUsuario") ? "AdminUsuarios" : null)
            })
            {
                model.AccesosAdministrativos.Add(acceso);
            }
            return View("AdminModulo", model);
        }

        // CAMBIO: el personal autorizado consulta y corrige pasajeros sin borrar su historial de reservas.
        [Authorize(Roles = "Administrador,SuperUsuario")]
        public ActionResult AdminPasajeros(string busqueda = null)
        {
            var model = CrearModeloAdministracionPasajeros(busqueda);
            if (model.Busqueda.Length > 100)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            if (string.IsNullOrWhiteSpace(model.Busqueda) || model.Busqueda.Length < 3)
            {
                model.Aviso = "Ingrese al menos 3 caracteres para buscar pasajeros.";
                return View("AdminPasajeros", model);
            }

            var termino = model.Busqueda;
            model.Pasajeros = db.Pasajeroes
                .Where(p => p.NumeroDocumento.Contains(termino) || p.Nombres.Contains(termino) ||
                    p.Apellidos.Contains(termino) || (p.Correo != null && p.Correo.Contains(termino)))
                .OrderBy(p => p.Apellidos)
                .ThenBy(p => p.Nombres)
                .Select(p => new PasajeroAdministracionItemViewModel
                {
                    PasajeroId = p.PasajeroId,
                    TipoDocumento = p.TipoDocumentoIdentidad.Descripcion,
                    NumeroDocumento = p.NumeroDocumento,
                    NombreCompleto = p.Nombres + " " + p.Apellidos,
                    Correo = p.Correo,
                    ReservasAsociadas = p.ReservaPasajeroVueloes.Select(a => a.ReservaId).Distinct().Count()
                })
                .ToList();
            model.Aviso = model.Pasajeros.Count == 0 ? "No se encontraron pasajeros con esos datos." : null;
            return View("AdminPasajeros", model);
        }

        [Authorize(Roles = "Administrador,SuperUsuario")]
        public ActionResult CrearPasajeroAdmin(string busqueda = null)
        {
            var model = CrearModeloAdministracionPasajeros(busqueda);
            model.Formulario = new PasajeroAdministracionFormularioViewModel();
            return View("AdminPasajeros", model);
        }

        [HttpPost]
        [Authorize(Roles = "Administrador,SuperUsuario")]
        [ValidateAntiForgeryToken]
        public ActionResult CrearPasajeroAdmin(AdministracionPasajerosViewModel model)
        {
            if (model == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            if (model.Formulario == null)
            {
                model.Formulario = new PasajeroAdministracionFormularioViewModel();
                ModelState.AddModelError("Formulario", "Los datos del pasajero son obligatorios.");
            }
            NormalizarPasajero(model.Formulario);
            ValidarPasajeroAdmin(model.Formulario);
            if (ModelState.IsValid)
            {
                var pasajero = new Pasajero
                {
                    TipoDocumentoCodigo = model.Formulario.TipoDocumentoCodigo,
                    NumeroDocumento = model.Formulario.NumeroDocumento,
                    Nombres = model.Formulario.Nombres,
                    Apellidos = model.Formulario.Apellidos,
                    FechaNacimiento = model.Formulario.FechaNacimiento,
                    Correo = model.Formulario.Correo,
                    Telefono = model.Formulario.Telefono,
                    FechaRegistro = DateTime.Now
                };
                db.Pasajeroes.Add(pasajero);
                db.SaveChanges();
                TempData["AvisoPasajeros"] = "Pasajero registrado correctamente.";
                return RedirectToAction("AdminPasajeros", new { busqueda = pasajero.NumeroDocumento });
            }

            PrepararModeloPasajeroAdmin(model);
            return View("AdminPasajeros", model);
        }

        [Authorize(Roles = "Administrador,SuperUsuario")]
        public ActionResult EditarPasajeroAdmin(int? id)
        {
            if (!id.HasValue)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var pasajero = db.Pasajeroes.Find(id.Value);
            if (pasajero == null)
            {
                return HttpNotFound();
            }

            var model = CrearModeloAdministracionPasajeros(pasajero.NumeroDocumento);
            model.Formulario = new PasajeroAdministracionFormularioViewModel
            {
                PasajeroId = pasajero.PasajeroId,
                TipoDocumentoCodigo = pasajero.TipoDocumentoCodigo,
                NumeroDocumento = pasajero.NumeroDocumento,
                Nombres = pasajero.Nombres,
                Apellidos = pasajero.Apellidos,
                FechaNacimiento = pasajero.FechaNacimiento,
                Correo = pasajero.Correo,
                Telefono = pasajero.Telefono
            };
            return View("AdminPasajeros", model);
        }

        [HttpPost]
        [Authorize(Roles = "Administrador,SuperUsuario")]
        [ValidateAntiForgeryToken]
        public ActionResult EditarPasajeroAdmin(AdministracionPasajerosViewModel model)
        {
            if (model == null || model.Formulario == null || !model.Formulario.PasajeroId.HasValue)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            NormalizarPasajero(model.Formulario);
            ValidarPasajeroAdmin(model.Formulario);
            if (ModelState.IsValid)
            {
                var pasajero = db.Pasajeroes.Find(model.Formulario.PasajeroId.Value);
                if (pasajero == null)
                {
                    return HttpNotFound();
                }
                pasajero.TipoDocumentoCodigo = model.Formulario.TipoDocumentoCodigo;
                pasajero.NumeroDocumento = model.Formulario.NumeroDocumento;
                pasajero.Nombres = model.Formulario.Nombres;
                pasajero.Apellidos = model.Formulario.Apellidos;
                pasajero.FechaNacimiento = model.Formulario.FechaNacimiento;
                pasajero.Correo = model.Formulario.Correo;
                pasajero.Telefono = model.Formulario.Telefono;
                db.SaveChanges();
                TempData["AvisoPasajeros"] = "Los datos del pasajero fueron actualizados.";
                return RedirectToAction("AdminPasajeros", new { busqueda = pasajero.NumeroDocumento });
            }

            PrepararModeloPasajeroAdmin(model);
            return View("AdminPasajeros", model);
        }

        private AdministracionPasajerosViewModel CrearModeloAdministracionPasajeros(string busqueda)
        {
            return new AdministracionPasajerosViewModel
            {
                Busqueda = (busqueda ?? string.Empty).Trim(),
                Aviso = TempData["AvisoPasajeros"] as string,
                TiposDocumento = CrearOpcionesTipoDocumento(null)
            };
        }

        private void PrepararModeloPasajeroAdmin(AdministracionPasajerosViewModel model)
        {
            model.TiposDocumento = CrearOpcionesTipoDocumento(model.Formulario.TipoDocumentoCodigo);
            model.Aviso = model.Formulario.PasajeroId.HasValue ? "Corrija los errores para actualizar el registro." : "Corrija los errores para registrar el pasajero.";
        }

        private IList<SelectListItem> CrearOpcionesTipoDocumento(string seleccionado)
        {
            return db.TipoDocumentoIdentidads.OrderBy(t => t.Descripcion)
                .Select(t => new SelectListItem
                {
                    Value = t.Codigo,
                    Text = t.Descripcion,
                    Selected = t.Codigo == seleccionado
                })
                .ToList();
        }

        private void ValidarPasajeroAdmin(PasajeroAdministracionFormularioViewModel formulario)
        {
            if (formulario == null)
            {
                ModelState.AddModelError("Formulario", "Los datos del pasajero son obligatorios.");
                return;
            }
            if (formulario.FechaNacimiento.HasValue && formulario.FechaNacimiento.Value.Date > DateTime.Today)
            {
                ModelState.AddModelError("Formulario.FechaNacimiento", "La fecha de nacimiento no puede ser futura.");
            }
            if (formulario.FechaNacimiento.HasValue && formulario.FechaNacimiento.Value.Date <= new DateTime(1900, 1, 1))
            {
                ModelState.AddModelError("Formulario.FechaNacimiento", "La fecha de nacimiento debe ser posterior al 01/01/1900.");
            }
            if (!db.TipoDocumentoIdentidads.Any(t => t.Codigo == formulario.TipoDocumentoCodigo))
            {
                ModelState.AddModelError("Formulario.TipoDocumentoCodigo", "El tipo de documento seleccionado no está disponible.");
            }
            if (db.Pasajeroes.Any(p => p.TipoDocumentoCodigo == formulario.TipoDocumentoCodigo &&
                p.NumeroDocumento == formulario.NumeroDocumento && p.PasajeroId != formulario.PasajeroId))
            {
                ModelState.AddModelError("Formulario.NumeroDocumento", "Ya existe un pasajero con ese tipo y número de documento.");
            }
        }

        private static void NormalizarPasajero(PasajeroAdministracionFormularioViewModel formulario)
        {
            if (formulario == null)
            {
                return;
            }
            formulario.TipoDocumentoCodigo = (formulario.TipoDocumentoCodigo ?? string.Empty).Trim();
            formulario.NumeroDocumento = (formulario.NumeroDocumento ?? string.Empty).Trim();
            formulario.Nombres = (formulario.Nombres ?? string.Empty).Trim();
            formulario.Apellidos = (formulario.Apellidos ?? string.Empty).Trim();
            formulario.Correo = string.IsNullOrWhiteSpace(formulario.Correo) ? null : formulario.Correo.Trim();
            formulario.Telefono = string.IsNullOrWhiteSpace(formulario.Telefono) ? null : formulario.Telefono.Trim();
        }

        // CAMBIO: Identity almacena usuarios y roles; solo SuperUsuario administra roles operativos.
        [Authorize(Roles = "SuperUsuario")]
        public ActionResult AdminUsuarios()
        {
            ViewBag.TitleModulo = "Usuarios y roles";
            ViewBag.NumeroPantalla = 20;
            var model = ObtenerResumenAdministrativo();
            ViewBag.Aviso = TempData["Aviso"] as string;
            using (var identityDb = new ApplicationDbContext())
            using (var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(identityDb)))
            {
                foreach (var usuario in identityDb.Users.OrderBy(u => u.UserName).ToList())
                {
                    var roles = userManager.GetRoles(usuario.Id);
                    model.UsuariosAdministrativos.Add(new UsuarioAdministrativoViewModel
                    {
                        Id = usuario.Id,
                        Nombre = usuario.UserName,
                        Correo = usuario.Email,
                        Roles = string.Join(", ", roles),
                        EsEditable = usuario.Id != User.Identity.GetUserId() && roles.All(r => r == "Cliente" || r == "Counter")
                    });
                }
            }

            return View("AdminModulo", model);
        }

        [HttpPost]
        [Authorize(Roles = "SuperUsuario")]
        [ValidateAntiForgeryToken]
        public ActionResult CambiarRolUsuario(string id, string rol)
        {
            if (string.IsNullOrWhiteSpace(id) || (rol != "Cliente" && rol != "Counter"))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            if (id == User.Identity.GetUserId())
            {
                TempData["Aviso"] = "No puedes cambiar el rol de tu propia cuenta.";
                return RedirectToAction("AdminUsuarios");
            }

            using (var identityDb = new ApplicationDbContext())
            using (var transaction = identityDb.Database.BeginTransaction(IsolationLevel.Serializable))
            using (var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(identityDb)))
            {
                var usuario = userManager.FindById(id);
                if (usuario == null)
                {
                    return HttpNotFound();
                }

                var rolesActuales = userManager.GetRoles(usuario.Id);
                if (rolesActuales.Any(r => r != "Cliente" && r != "Counter"))
                {
                    TempData["Aviso"] = "Las cuentas administrativas solo se cambian mediante la configuración local de inicio.";
                    transaction.Commit();
                    return RedirectToAction("AdminUsuarios");
                }

                if (!userManager.IsInRole(usuario.Id, rol))
                {
                    VerificarResultadoIdentity(userManager.AddToRole(usuario.Id, rol), "asignar el rol " + rol);
                }
                foreach (var rolActual in rolesActuales.Where(r => r != rol))
                {
                    VerificarResultadoIdentity(userManager.RemoveFromRole(usuario.Id, rolActual), "retirar el rol " + rolActual);
                }

                // CAMBIO: fuerza la renovación de las cookies Identity al cambiar permisos del usuario.
                VerificarResultadoIdentity(userManager.UpdateSecurityStamp(usuario.Id), "invalidar la sesión anterior");
                transaction.Commit();
            }

            TempData["Aviso"] = "Rol actualizado. El usuario debe iniciar sesión nuevamente para renovar su sesión y permisos.";
            return RedirectToAction("AdminUsuarios");
        }

        private static void VerificarResultadoIdentity(IdentityResult resultado, string operacion)
        {
            if (!resultado.Succeeded)
            {
                throw new InvalidOperationException("No se pudo " + operacion + ": " + string.Join("; ", resultado.Errors));
            }
        }

        // CAMBIO: agrega filtros por fecha y agrupaciones reales de reservas y compras.
        [Authorize(Roles = "view")]
        public ActionResult AdminReportes(DateTime? desde, DateTime? hasta)
        {
            var model = ObtenerResumenAdministrativo();
            model.ReporteDesde = desde;
            model.ReporteHasta = hasta;
            if ((desde.HasValue && hasta.HasValue && desde.Value.Date > hasta.Value.Date) ||
                (hasta.HasValue && hasta.Value.Date == DateTime.MaxValue.Date))
            {
                model.ReporteAviso = "El rango de fechas no es válido.";
                return View("Reportes", model);
            }

            var reservas = db.Reservas.AsQueryable();
            var compras = db.Compras.AsQueryable();
            if (desde.HasValue)
            {
                reservas = reservas.Where(r => r.FechaCreacion >= desde.Value.Date);
                compras = compras.Where(c => c.FechaCompra >= desde.Value.Date);
            }
            if (hasta.HasValue)
            {
                var limiteSuperior = hasta.Value.Date.AddDays(1);
                reservas = reservas.Where(r => r.FechaCreacion < limiteSuperior);
                compras = compras.Where(c => c.FechaCompra < limiteSuperior);
            }

            model.ReservasPorEstado = reservas
                .GroupBy(r => r.EstadoReservaCodigo)
                .Select(g => new EstadoReporteViewModel { Estado = g.Key, Total = g.Count() })
                .OrderBy(x => x.Estado)
                .ToList();
            model.ComprasPorEstado = compras
                .GroupBy(c => c.EstadoCompraCodigo)
                .Select(g => new EstadoReporteViewModel { Estado = g.Key, Total = g.Count() })
                .OrderBy(x => x.Estado)
                .ToList();
            return View("Reportes", model);
        }

        private PagoSimuladoViewModel CrearModeloPago(Reserva reserva)
        {
            var compra = reserva.Compras.OrderByDescending(c => c.FechaCompra).FirstOrDefault();
            var asignaciones = reserva.ReservaPasajeroVueloes.ToList();
            var primera = asignaciones.OrderBy(a => a.Vuelo.FechaOperacion).FirstOrDefault();
            var modelo = new PagoSimuladoViewModel
            {
                ReservaId = reserva.ReservaId,
                CodigoReserva = reserva.CodigoReserva,
                Total = compra == null ? asignaciones.Sum(a => a.ImporteTramo) : compra.Importe,
                MonedaCodigo = compra == null ? "USD" : compra.MonedaCodigo,
                EstadoCompra = compra == null ? "Sin compra registrada" : compra.EstadoCompra.Descripcion,
                CantidadPasajeros = asignaciones.Select(a => a.PasajeroId).Distinct().Count(),
                Asientos = string.Join(", ", asignaciones.Select(a => a.NumeroAsiento.ToString()).Distinct())
            };

            if (primera != null)
            {
                var ruta = primera.Vuelo.Frecuencia.Ruta;
                var origen = db.Aeropuertoes.Find(ruta.AeropuertoOrigenId);
                var destino = db.Aeropuertoes.Find(ruta.AeropuertoDestinoId);
                modelo.Ruta = origen.Localidad + " → " + destino.Localidad;
                modelo.FechaViaje = primera.Vuelo.FechaOperacion;
                modelo.Horario = primera.Vuelo.Frecuencia.HoraSalida.ToString(@"hh\:mm") + "–" + primera.Vuelo.Frecuencia.HoraLlegada.ToString(@"hh\:mm");
            }

            CargarOpcionesPago(modelo);
            return modelo;
        }

        private void CargarOpcionesPago(PagoSimuladoViewModel model)
        {
            model.MetodosPago = db.MetodoPagoes
                .Where(m => m.Activo)
                .OrderBy(m => m.Nombre)
                .ToList()
                .Select(m => new MetodoPagoOpcionViewModel { Codigo = m.Codigo, Nombre = m.Nombre })
                .ToList();
            model.MarcasTarjeta = db.MarcaTarjetas
                .Where(m => m.Activa)
                .OrderBy(m => m.Nombre)
                .ToList()
                .Select(m => new MarcaTarjetaOpcionViewModel
                {
                    Codigo = m.Codigo,
                    Nombre = m.Nombre,
                    LongitudNumero = m.LongitudNumero,
                    LongitudCodigoSeguridad = m.LongitudCodigoSeguridad
                })
                .ToList();
        }

        private void CargarTiposDocumento(InterfacesViewModel model)
        {
            model.TiposDocumento = db.TipoDocumentoIdentidads
                .OrderBy(t => t.Descripcion)
                .ToList()
                .Select(t => new SelectListItem { Value = t.Codigo, Text = t.Descripcion })
                .ToList();
        }

        private void CargarAeropuertos(InterfacesViewModel model)
        {
            model.Aeropuertos = db.Aeropuertoes
                .Where(a => a.Activo)
                .OrderBy(a => a.Localidad)
                .ToList()
                .Select(a => new SelectListItem
                {
                    Value = a.CodigoIATA,
                    Text = a.Localidad + " (" + a.CodigoIATA + ")",
                    Selected = a.CodigoIATA == model.Origen
                })
                .ToList();
        }

        // CAMBIO: la tabla de días/horarios se alimenta de las frecuencias activas de cada ruta.
        private void CargarRutasOperacion(InterfacesViewModel model, int? aeropuertoOrigenId)
        {
            var aeropuertosActivos = db.Aeropuertoes.Where(a => a.Activo).Select(a => a.AeropuertoId).ToList();
            var rutas = db.Rutas
                .Where(r => r.Activa && aeropuertosActivos.Contains(r.AeropuertoOrigenId) &&
                    aeropuertosActivos.Contains(r.AeropuertoDestinoId));
            if (aeropuertoOrigenId.HasValue)
            {
                rutas = rutas.Where(r => r.AeropuertoOrigenId == aeropuertoOrigenId.Value);
            }

            var datosRutas = rutas.Select(r => new
            {
                r.RutaId,
                r.AeropuertoOrigenId,
                r.AeropuertoDestinoId
            }).ToList();
            var rutaIds = datosRutas.Select(r => r.RutaId).ToList();
            var frecuencias = db.Frecuencias
                .Where(f => f.Activa && rutaIds.Contains(f.RutaId))
                .Select(f => new { f.RutaId, f.DiaSemana, f.HoraSalida, f.HoraLlegada })
                .ToList();
            var aeropuertos = db.Aeropuertoes
                .Where(a => aeropuertosActivos.Contains(a.AeropuertoId))
                .ToDictionary(a => a.AeropuertoId);
            var nombresDias = new[] { "Lunes", "Martes", "Miercoles", "Jueves", "Viernes", "Sabado", "Domingo" };

            foreach (var ruta in datosRutas)
            {
                var frecuenciasRuta = frecuencias.Where(f => f.RutaId == ruta.RutaId).ToList();
                if (frecuenciasRuta.Count == 0)
                {
                    continue;
                }

                model.RutasOperacion.Add(new RutaOperacionViewModel
                {
                    Ruta = aeropuertos[ruta.AeropuertoOrigenId].Localidad + " (" + aeropuertos[ruta.AeropuertoOrigenId].CodigoIATA + ") → " +
                        aeropuertos[ruta.AeropuertoDestinoId].Localidad + " (" + aeropuertos[ruta.AeropuertoDestinoId].CodigoIATA + ")",
                    DiasHorarios = string.Join("; ", frecuenciasRuta.GroupBy(f => (int)f.DiaSemana).OrderBy(g => g.Key)
                        .Where(g => g.Key >= 1 && g.Key <= 7)
                        .Select(g => nombresDias[g.Key - 1] + ": " + string.Join(", ", g.OrderBy(f => f.HoraSalida)
                            .Select(f => f.HoraSalida.ToString(@"hh\:mm") + "–" + f.HoraLlegada.ToString(@"hh\:mm")).Distinct())))
                });
            }
        }

        private void CargarDestinos(InterfacesViewModel model, int? aeropuertoOrigenId)
        {
            var rutas = db.Rutas.Where(r => r.Activa && r.Frecuencias.Any(f => f.Activa));
            if (aeropuertoOrigenId.HasValue)
            {
                rutas = rutas.Where(r => r.AeropuertoOrigenId == aeropuertoOrigenId.Value);
            }

            var destinos = rutas.Select(r => r.AeropuertoDestinoId).Distinct().ToList();
            model.Destinos = db.Aeropuertoes
                .Where(a => a.Activo && destinos.Contains(a.AeropuertoId))
                .OrderBy(a => a.Localidad)
                .ToList()
                .Select(a => new SelectListItem
                {
                    Value = a.CodigoIATA,
                    Text = a.Localidad + " (" + a.CodigoIATA + ")",
                    Selected = a.CodigoIATA == model.Destino
                })
                .ToList();
        }

        private Aeropuerto BuscarAeropuerto(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo))
            {
                return null;
            }

            var criterio = codigo.Trim();
            return db.Aeropuertoes
                .Where(a => a.Activo)
                .ToList()
                .FirstOrDefault(a => string.Equals(a.CodigoIATA, criterio, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(a.Localidad, criterio, StringComparison.OrdinalIgnoreCase));
        }

        private Vuelo CargarVueloConAsientos(int vueloId)
        {
            return db.Vueloes
                .Include("Frecuencia.Ruta")
                .Include("Aeronave.ModeloAeronave.Asientoes")
                .Include("EstadoVuelo")
                .FirstOrDefault(v => v.VueloId == vueloId);
        }

        private InterfacesViewModel CrearModeloAsientos(Vuelo vuelo, bool permiteSeleccion)
        {
            var model = new InterfacesViewModel
            {
                VueloId = vuelo.VueloId,
                ModeloAeronave = vuelo.Aeronave == null ? "Aeronave sin asignar" : vuelo.Aeronave.ModeloAeronave.Fabricante + " " + vuelo.Aeronave.ModeloAeronave.Nombre,
                FechaViaje = vuelo.FechaOperacion,
                CantidadPasajeros = Session["CantidadSolicitada"] is int ? (int)Session["CantidadSolicitada"] : 1
            };

            if (vuelo.Aeronave == null)
            {
                model.Mensaje = "Este vuelo aún no tiene aeronave asignada y no puede comercializarse.";
                return model;
            }

            var ahora = DateTime.Now;
            var asignaciones = db.ReservaPasajeroVueloes
                .Include("EstadoAsignacion")
                .Where(a => a.VueloId == vuelo.VueloId &&
                            (a.EstadoAsignacionCodigo == "CONFIRMADA" ||
                             (a.EstadoAsignacionCodigo == "PENDIENTE" &&
                              a.Reserva.FechaExpiracion.HasValue && a.Reserva.FechaExpiracion > ahora)))
                .ToList();

            foreach (var asiento in vuelo.Aeronave.ModeloAeronave.Asientoes.OrderBy(a => a.NumeroAsiento))
            {
                var asignacion = asignaciones.FirstOrDefault(a => a.NumeroAsiento == asiento.NumeroAsiento);
                var estadoVisual = asignacion == null
                    ? "DISPONIBLE"
                    : asignacion.EstadoAsignacion.EstadoVisual;
                if (asignacion != null && string.Equals(estadoVisual, "DISPONIBLE", StringComparison.OrdinalIgnoreCase))
                {
                    estadoVisual = asignacion.EstadoAsignacionCodigo == "PENDIENTE" ? "RESERVADO" : "OCUPADO";
                }

                var disponible = asignacion == null &&
                    string.Equals(estadoVisual, "DISPONIBLE", StringComparison.OrdinalIgnoreCase);
                var estadoTexto = string.Equals(estadoVisual, "DISPONIBLE", StringComparison.OrdinalIgnoreCase)
                    ? "Disponible"
                    : string.Equals(estadoVisual, "RESERVADO", StringComparison.OrdinalIgnoreCase)
                        ? "Reservado"
                        : "Ocupado";
                model.Asientos.Add(new AsientoInterfazViewModel
                {
                    Numero = asiento.NumeroAsiento,
                    Fila = asiento.Fila,
                    Codigo = asiento.Codigo,
                    Ubicacion = asiento.Ubicacion,
                    Estado = estadoTexto,
                    EstadoVisual = estadoVisual,
                    Seleccionable = permiteSeleccion && disponible,
                    Seleccionado = false
                });
            }

            if (!permiteSeleccion)
            {
                model.Mensaje = "La selección de la pantalla siguiente es temporal; el servidor volverá a comprobar los asientos.";
            }

            return model;
        }

        private InterfacesViewModel CrearModeloBorrador()
        {
            var vueloId = Session["VueloSeleccionadoId"] as int?;
            var seleccionados = Session["AsientosSeleccionados"] as List<string>;
            if (!vueloId.HasValue || seleccionados == null || seleccionados.Count == 0)
            {
                return null;
            }

            var vuelo = CargarVueloConAsientos(vueloId.Value);
            if (vuelo == null)
            {
                return null;
            }

            var model = CrearModeloAsientos(vuelo, false);
            var pasajerosGuardados = Session["PasajerosBorrador"] as List<PasajeroInterfazViewModel>;
            model.Pasajeros = seleccionados.Select((asiento, index) =>
            {
                var pasajero = pasajerosGuardados != null && index < pasajerosGuardados.Count
                    ? pasajerosGuardados[index]
                    : new PasajeroInterfazViewModel();
                pasajero.Numero = index + 1;
                pasajero.Asiento = asiento;
                return pasajero;
            }).ToList();
            return model;
        }

        private bool DatosPasajerosCompletos(InterfacesViewModel borrador)
        {
            if (borrador == null || borrador.Pasajeros == null || borrador.Pasajeros.Count == 0)
            {
                return false;
            }

            if (borrador.Pasajeros.Any(p => p == null))
            {
                return false;
            }

            var codigosDocumento = new HashSet<string>(db.TipoDocumentoIdentidads.Select(t => t.Codigo).ToList(), StringComparer.OrdinalIgnoreCase);
            return borrador.Pasajeros.All(p =>
                !string.IsNullOrWhiteSpace(p.Nombres) && p.Nombres.Trim().Length <= 100
                && !string.IsNullOrWhiteSpace(p.Apellidos) && p.Apellidos.Trim().Length <= 100
                && !string.IsNullOrWhiteSpace(p.TipoDocumento) && codigosDocumento.Contains(p.TipoDocumento)
                && !string.IsNullOrWhiteSpace(p.Documento) && p.Documento.Trim().Length >= 5 && p.Documento.Trim().Length <= 20)
                && !borrador.Pasajeros
                    .GroupBy(p => p.TipoDocumento + ":" + p.Documento.Trim(), StringComparer.OrdinalIgnoreCase)
                    .Any(grupo => grupo.Count() > 1);
        }

        private ReservaConfirmacionViewModel CrearModeloConfirmacion()
        {
            var confirmacion = new ReservaConfirmacionViewModel
            {
                MonedaCodigo = "USD",
                CondicionesReserva = "La reserva queda pendiente de pago hasta por " + LeerParametroPositivo("ReservaRetencionMinutos") + " minutos o hasta la salida del vuelo, lo que ocurra primero. Si vence el plazo, los asientos se liberan."
            };

            var borrador = CrearModeloBorrador();
            if (borrador == null)
            {
                return confirmacion;
            }

            var vueloId = (int)Session["VueloSeleccionadoId"];
            var vuelo = CargarVueloConAsientos(vueloId);
            if (vuelo == null)
            {
                return confirmacion;
            }

            var aeropuertoOrigen = db.Aeropuertoes.Find(vuelo.Frecuencia.Ruta.AeropuertoOrigenId);
            var aeropuertoDestino = db.Aeropuertoes.Find(vuelo.Frecuencia.Ruta.AeropuertoDestinoId);
            var ruta = aeropuertoOrigen.Localidad + " → " + aeropuertoDestino.Localidad;
            confirmacion.Tramos.Add(new TramoReservaViewModel
            {
                Orden = 1,
                CodigoVuelo = "VIP" + vuelo.VueloId.ToString("D5"),
                Ruta = ruta,
                Fecha = vuelo.FechaOperacion,
                Horario = vuelo.Frecuencia.HoraSalida.ToString(@"hh\:mm") + "–" + vuelo.Frecuencia.HoraLlegada.ToString(@"hh\:mm")
            });

            foreach (var pasajero in borrador.Pasajeros)
            {
                confirmacion.Pasajeros.Add(new PasajeroReservaViewModel
                {
                    NombreCompleto = string.IsNullOrWhiteSpace(pasajero.NombreCompleto) ? "Pasajero " + pasajero.Numero : pasajero.NombreCompleto,
                    TipoDocumento = pasajero.TipoDocumento,
                    DocumentoEnmascarado = EnmascararDocumento(pasajero.Documento),
                    AsientosPorTramo = new List<string> { pasajero.Asiento },
                    ImporteEstimado = vuelo.Frecuencia.Ruta.TarifaReferencial
                });
                confirmacion.TotalEstimado += vuelo.Frecuencia.Ruta.TarifaReferencial;
            }

            return confirmacion;
        }

        private Reserva ObtenerReservaPropia(int reservaId)
        {
            var usuarioId = User.Identity.GetUserId();
            var puedeAsistir = User.IsInRole("Counter") || User.IsInRole("view");
            return db.Reservas
                .Include("EstadoReserva")
                .Include("ReservaPasajeroVueloes.Pasajero")
                .Include("ReservaPasajeroVueloes.Vuelo.Frecuencia.Ruta")
                .Include("Compras.EstadoCompra")
                .Include("Compras.Pagoes.EstadoPago")
                .FirstOrDefault(r => r.ReservaId == reservaId && (r.UsuarioId == usuarioId || puedeAsistir));
        }

        private ReservaInterfazViewModel CrearModeloReserva(Reserva reserva)
        {
            var asignaciones = reserva.ReservaPasajeroVueloes.ToList();
            var compra = reserva.Compras.OrderByDescending(c => c.FechaCompra).FirstOrDefault();
            var primeraAsignacion = asignaciones.FirstOrDefault();
            var model = new ReservaInterfazViewModel
            {
                Tramos = new List<TramoInterfazViewModel>(),
                ReservaId = reserva.ReservaId,
                CodigoReserva = reserva.CodigoReserva,
                FechaCreacion = reserva.FechaCreacion,
                CantidadPasajeros = asignaciones.Select(a => a.PasajeroId).Distinct().Count(),
                EstadoReserva = ReservaPendienteVencida(reserva, DateTime.Now) ? "Vencida" : reserva.EstadoReserva.Descripcion,
                EstadoCompra = compra == null ? "Sin compra" : compra.EstadoCompra.Descripcion,
                PermitePago = reserva.EstadoReservaCodigo == "PENDIENTE" && !ReservaPendienteVencida(reserva, DateTime.Now),
                PermiteCancelar = reserva.EstadoReservaCodigo == "CONFIRMADA" ||
                    (reserva.EstadoReservaCodigo == "PENDIENTE" && !ReservaPendienteVencida(reserva, DateTime.Now)),
                Importe = compra == null ? asignaciones.Sum(a => a.ImporteTramo) : compra.Importe,
                Moneda = compra == null ? "USD" : compra.MonedaCodigo
            };

            if (primeraAsignacion != null)
            {
                var vuelo = primeraAsignacion.Vuelo;
                var origen = db.Aeropuertoes.Find(vuelo.Frecuencia.Ruta.AeropuertoOrigenId);
                var destino = db.Aeropuertoes.Find(vuelo.Frecuencia.Ruta.AeropuertoDestinoId);
                model.FechaViaje = vuelo.FechaOperacion;
                model.Ruta = origen.Localidad + " → " + destino.Localidad;

                foreach (var asignacion in asignaciones.OrderBy(a => a.Vuelo.FechaOperacion).ThenBy(a => a.Vuelo.Frecuencia.HoraSalida))
                {
                    var aeropuertoOrigen = db.Aeropuertoes.Find(asignacion.Vuelo.Frecuencia.Ruta.AeropuertoOrigenId);
                    var aeropuertoDestino = db.Aeropuertoes.Find(asignacion.Vuelo.Frecuencia.Ruta.AeropuertoDestinoId);
                    model.Tramos.Add(new TramoInterfazViewModel
                    {
                        Orden = asignacion.Vuelo.TramoItinerarios.FirstOrDefault() == null ? 1 : asignacion.Vuelo.TramoItinerarios.First().Orden,
                        CodigoVuelo = "VIP" + asignacion.VueloId.ToString("D5"),
                        Ruta = aeropuertoOrigen.Localidad + " → " + aeropuertoDestino.Localidad,
                        Fecha = asignacion.Vuelo.FechaOperacion,
                        Horario = asignacion.Vuelo.Frecuencia.HoraSalida.ToString(@"hh\:mm") + "–" + asignacion.Vuelo.Frecuencia.HoraLlegada.ToString(@"hh\:mm"),
                        NombrePasajero = asignacion.Pasajero == null ? "Pasajero" : asignacion.Pasajero.Nombres + " " + asignacion.Pasajero.Apellidos,
                        Asiento = asignacion.NumeroAsiento.ToString()
                    });
                }
            }

            return model;
        }

        private InterfacesViewModel ObtenerResumenAdministrativo()
        {
            return new InterfacesViewModel
            {
                TotalVuelos = db.Vueloes.Count(),
                TotalAeronaves = db.Aeronaves.Count(),
                TotalReservas = db.Reservas.Count(),
                TotalPasajeros = db.Pasajeroes.Count(),
                TotalCompras = db.Compras.Count(),
                TotalComprobantes = db.Comprobantes.Count()
            };
        }

        private static AccesoAdministrativoViewModel Acceso(string titulo, string descripcion, string controlador, string accion)
        {
            return new AccesoAdministrativoViewModel
            {
                Titulo = titulo,
                Descripcion = descripcion,
                Controlador = controlador,
                Accion = accion
            };
        }

        private static string EnmascararDocumento(string documento)
        {
            if (string.IsNullOrWhiteSpace(documento) || documento.Length <= 2)
            {
                return "—";
            }

            return new string('•', documento.Length - 2) + documento.Substring(documento.Length - 2);
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
