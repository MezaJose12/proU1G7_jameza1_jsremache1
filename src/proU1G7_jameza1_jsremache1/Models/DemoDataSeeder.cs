using System;
using System.Linq;

namespace proU1G7_jameza1_jsremache1.Models
{
    // Registros sintéticos para probar la interfaz en desarrollo. No representan operaciones reales.
    public static class DemoDataSeeder
    {
        public static void Seed(string usuarioId)
        {
            using (var db = new AerolineaVIPEntities())
            using (var transaction = db.Database.BeginTransaction())
            {
                EnsureCatalogos(db);
                db.SaveChanges();

                var quito = EnsureAeropuerto(db, "UIO", "Aeropuerto Internacional Mariscal Sucre", "Quito");
                var guayaquil = EnsureAeropuerto(db, "GYE", "Aeropuerto Internacional José Joaquín de Olmedo", "Guayaquil");
                var ruta = db.Rutas.FirstOrDefault(r => r.AeropuertoOrigenId == quito.AeropuertoId &&
                                                       r.AeropuertoDestinoId == guayaquil.AeropuertoId);
                if (ruta == null)
                {
                    ruta = new Ruta
                    {
                        AeropuertoOrigenId = quito.AeropuertoId,
                        AeropuertoDestinoId = guayaquil.AeropuertoId,
                        TarifaReferencial = 185.00m,
                        Activa = true
                    };
                    db.Rutas.Add(ruta);
                    db.SaveChanges();
                }

                var modelo = db.ModeloAeronaves.FirstOrDefault(m => m.Fabricante == "DEMO" && m.Nombre == "VIP 7");
                if (modelo == null)
                {
                    modelo = new ModeloAeronave { Fabricante = "DEMO", Nombre = "VIP 7", CapacidadAsientos = 7 };
                    db.ModeloAeronaves.Add(modelo);
                    db.SaveChanges();
                }

                EnsureAsientos(db, modelo.ModeloAeronaveId);
                var aeronave = db.Aeronaves.FirstOrDefault(a => a.Matricula == "DEMO-001");
                if (aeronave != null && aeronave.ModeloAeronaveId != modelo.ModeloAeronaveId)
                {
                    throw new InvalidOperationException("La matrícula DEMO-001 ya está asociada a un modelo diferente.");
                }
                if (aeronave == null)
                {
                    aeronave = new Aeronave
                    {
                        Matricula = "DEMO-001",
                        ModeloAeronaveId = modelo.ModeloAeronaveId,
                        EstadoAeronaveCodigo = "DISPONIBLE"
                    };
                    db.Aeronaves.Add(aeronave);
                    db.SaveChanges();
                }

                var vuelos = new Vuelo[14];
                for (var dia = 1; dia <= vuelos.Length; dia++)
                {
                    var fecha = DateTime.Today.AddDays(dia);
                    var frecuencia = EnsureFrecuencia(db, ruta.RutaId, fecha.DayOfWeek);
                    var vuelo = db.Vueloes.FirstOrDefault(v => v.FrecuenciaId == frecuencia.FrecuenciaId &&
                                                             v.FechaOperacion == fecha);
                    if (vuelo == null)
                    {
                        vuelo = new Vuelo
                        {
                            FrecuenciaId = frecuencia.FrecuenciaId,
                            FechaOperacion = fecha,
                            AeronaveId = aeronave.AeronaveId,
                            EstadoVueloCodigo = "PROGRAMADO"
                        };
                        db.Vueloes.Add(vuelo);
                        db.SaveChanges();
                    }
                    vuelos[dia - 1] = vuelo;
                }

                if (!string.IsNullOrWhiteSpace(usuarioId))
                {
                    EnsureDemoReservation(db, usuarioId, vuelos[1], "DEMO-000001", "Pasajero", "Demostración", 1, "CONFIRMADA");
                    EnsureDemoReservation(db, usuarioId, vuelos[2], "DEMO-000002", "Pasajera", "Ejemplo", 2, "PENDIENTE");
                    db.SaveChanges();
                }

                transaction.Commit();
            }
        }

        private static void EnsureCatalogos(AerolineaVIPEntities db)
        {
            EnsureEstadoReserva(db, "CONFIRMADA", "Confirmada");
            EnsureEstadoReserva(db, "PENDIENTE", "Pendiente");
            EnsureEstadoAsignacion(db, "CONFIRMADA", "Confirmada", true, "OCUPADO");
            EnsureEstadoAsignacion(db, "PENDIENTE", "Pendiente", true, "RESERVADO");
            EnsureEstadoVuelo(db, "PROGRAMADO", "Programado");
            EnsureEstadoAeronave(db, "DISPONIBLE", "Disponible");

            if (!db.TipoDocumentoIdentidads.Any(t => t.Codigo == "CEDULA"))
            {
                db.TipoDocumentoIdentidads.Add(new TipoDocumentoIdentidad { Codigo = "CEDULA", Descripcion = "Cédula" });
            }
            if (!db.TipoDocumentoIdentidads.Any(t => t.Codigo == "PASAPORTE"))
            {
                db.TipoDocumentoIdentidads.Add(new TipoDocumentoIdentidad { Codigo = "PASAPORTE", Descripcion = "Pasaporte" });
            }
            if (!db.Monedas.Any(m => m.Codigo == "USD"))
            {
                db.Monedas.Add(new Moneda { Codigo = "USD", Descripcion = "Dólar estadounidense" });
            }
            if (!db.MetodoPagoes.Any(m => m.Codigo == "TARJETA"))
            {
                db.MetodoPagoes.Add(new MetodoPago { Codigo = "TARJETA", Nombre = "Tarjeta (demo)", Activo = true });
            }
            if (!db.MarcaTarjetas.Any(m => m.Codigo == "VISA"))
            {
                db.MarcaTarjetas.Add(new MarcaTarjeta
                {
                    Codigo = "VISA",
                    Nombre = "Visa (demo)",
                    LongitudNumero = 16,
                    LongitudCodigoSeguridad = 3,
                    PrefijosReferencia = "4",
                    RutaLogo = string.Empty,
                    Activa = true
                });
            }
        }

        private static Frecuencia EnsureFrecuencia(AerolineaVIPEntities db, int rutaId, DayOfWeek dia)
        {
            var diaSemana = (byte)(((int)dia + 6) % 7 + 1);
            var frecuencia = db.Frecuencias.FirstOrDefault(f => f.RutaId == rutaId && f.DiaSemana == diaSemana && f.Activa);
            if (frecuencia == null)
            {
                frecuencia = new Frecuencia
                {
                    RutaId = rutaId,
                    DiaSemana = diaSemana,
                    HoraSalida = new TimeSpan(9, 0, 0),
                    HoraLlegada = new TimeSpan(9, 50, 0),
                    Activa = true
                };
                db.Frecuencias.Add(frecuencia);
                db.SaveChanges();
            }
            return frecuencia;
        }

        private static Aeropuerto EnsureAeropuerto(AerolineaVIPEntities db, string codigo, string nombre, string localidad)
        {
            var aeropuerto = db.Aeropuertoes.FirstOrDefault(a => a.CodigoIATA == codigo);
            if (aeropuerto == null)
            {
                aeropuerto = new Aeropuerto
                {
                    CodigoIATA = codigo,
                    Nombre = nombre,
                    Localidad = localidad,
                    Activo = true
                };
                db.Aeropuertoes.Add(aeropuerto);
                db.SaveChanges();
            }
            return aeropuerto;
        }

        private static void EnsureAsientos(AerolineaVIPEntities db, int modeloId)
        {
            var asientosDemo = new[]
            {
                new { Numero = (byte)1, Codigo = "1A", Fila = (byte)1, Lado = "I", Ubicacion = "Ventana izquierda" },
                new { Numero = (byte)2, Codigo = "1B", Fila = (byte)1, Lado = "D", Ubicacion = "Ventana derecha" },
                new { Numero = (byte)3, Codigo = "2A", Fila = (byte)2, Lado = "I", Ubicacion = "Ventana izquierda" },
                new { Numero = (byte)4, Codigo = "2B", Fila = (byte)2, Lado = "D", Ubicacion = "Ventana derecha" },
                new { Numero = (byte)5, Codigo = "3A", Fila = (byte)3, Lado = "I", Ubicacion = "Ventana izquierda" },
                new { Numero = (byte)6, Codigo = "3B", Fila = (byte)3, Lado = "D", Ubicacion = "Ventana derecha" },
                new { Numero = (byte)7, Codigo = "4A", Fila = (byte)4, Lado = "I", Ubicacion = "Ventana izquierda" }
            };

            foreach (var item in asientosDemo)
            {
                if (!db.Asientoes.Any(a => a.ModeloAeronaveId == modeloId && a.NumeroAsiento == item.Numero))
                {
                    db.Asientoes.Add(new Asiento
                    {
                        ModeloAeronaveId = modeloId,
                        NumeroAsiento = item.Numero,
                        Codigo = item.Codigo,
                        Fila = item.Fila,
                        Lado = item.Lado,
                        Ubicacion = item.Ubicacion
                    });
                }
            }
                db.SaveChanges();
        }

        private static void EnsureDemoReservation(
            AerolineaVIPEntities db,
            string usuarioId,
            Vuelo vuelo,
            string documento,
            string nombres,
            string apellidos,
            byte numeroAsiento,
            string estado)
        {
            var reservaExistente = db.Reservas.FirstOrDefault(r => r.UsuarioId == usuarioId &&
                r.ReservaPasajeroVueloes.Any(a => a.Pasajero.NumeroDocumento == documento));
            if (reservaExistente != null)
            {
                if (estado == "PENDIENTE" && !reservaExistente.FechaExpiracion.HasValue)
                {
                    reservaExistente.FechaExpiracion = DateTime.Now.AddMinutes(15);
                    db.SaveChanges();
                }
                return;
            }

            var pasajero = db.Pasajeroes.FirstOrDefault(p => p.NumeroDocumento == documento);
            if (pasajero == null)
            {
                pasajero = new Pasajero
                {
                    TipoDocumentoCodigo = "CEDULA",
                    NumeroDocumento = documento,
                    Nombres = nombres,
                    Apellidos = apellidos,
                    Correo = "demo@aerolineavip.invalid",
                    FechaRegistro = DateTime.Now
                };
                db.Pasajeroes.Add(pasajero);
                db.SaveChanges();
            }

            var fechaCreacion = DateTime.Now;
            var reserva = new Reserva
            {
                UsuarioId = usuarioId,
                FechaCreacion = fechaCreacion,
                FechaExpiracion = estado == "PENDIENTE" ? fechaCreacion.AddMinutes(15) : (DateTime?)null,
                EstadoReservaCodigo = estado
            };
            db.Reservas.Add(reserva);
            db.SaveChanges();

            db.ReservaPasajeroVueloes.Add(new ReservaPasajeroVuelo
            {
                ReservaId = reserva.ReservaId,
                PasajeroId = pasajero.PasajeroId,
                VueloId = vuelo.VueloId,
                NumeroAsiento = numeroAsiento,
                ImporteTramo = 185.00m,
                EstadoAsignacionCodigo = estado,
                FechaAsignacion = DateTime.Now
            });
        }

        private static void EnsureEstadoReserva(AerolineaVIPEntities db, string codigo, string descripcion)
        {
            if (!db.EstadoReservas.Any(e => e.Codigo == codigo))
            {
                db.EstadoReservas.Add(new EstadoReserva { Codigo = codigo, Descripcion = descripcion });
            }
        }

        private static void EnsureEstadoAsignacion(AerolineaVIPEntities db, string codigo, string descripcion, bool ocupaInventario, string estadoVisual)
        {
            if (!db.EstadoAsignacions.Any(e => e.Codigo == codigo))
            {
                db.EstadoAsignacions.Add(new EstadoAsignacion
                {
                    Codigo = codigo,
                    Descripcion = descripcion,
                    OcupaInventario = ocupaInventario,
                    EstadoVisual = estadoVisual
                });
            }
        }

        private static void EnsureEstadoVuelo(AerolineaVIPEntities db, string codigo, string descripcion)
        {
            if (!db.EstadoVueloes.Any(e => e.Codigo == codigo))
            {
                db.EstadoVueloes.Add(new EstadoVuelo { Codigo = codigo, Descripcion = descripcion });
            }
        }

        private static void EnsureEstadoAeronave(AerolineaVIPEntities db, string codigo, string descripcion)
        {
            if (!db.EstadoAeronaves.Any(e => e.Codigo == codigo))
            {
                db.EstadoAeronaves.Add(new EstadoAeronave { Codigo = codigo, Descripcion = descripcion });
            }
        }
    }
}
