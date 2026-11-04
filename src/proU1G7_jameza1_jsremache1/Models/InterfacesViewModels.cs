using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace proU1G7_jameza1_jsremache1.Models
{
    // Modelos de presentación para las pantallas del mapa de navegación.
    // Las operaciones administrativas y las transacciones siguen teniendo sus propios controladores.
    public class InterfacesViewModel
    {
        public InterfacesViewModel()
        {
            Aeropuertos = new List<SelectListItem>();
            Destinos = new List<SelectListItem>();
            TiposDocumento = new List<SelectListItem>();
            Vuelos = new List<VueloTarjetaViewModel>();
            Itinerarios = new List<ItinerarioTarjetaViewModel>();
            Asientos = new List<AsientoInterfazViewModel>();
            Pasajeros = new List<PasajeroInterfazViewModel>();
            Reservas = new List<ReservaInterfazViewModel>();
            Tramos = new List<TramoInterfazViewModel>();
            AccesosAdministrativos = new List<AccesoAdministrativoViewModel>();
            UsuariosAdministrativos = new List<UsuarioAdministrativoViewModel>();
            RutasOperacion = new List<RutaOperacionViewModel>();
            ReservasPorEstado = new List<EstadoReporteViewModel>();
            ComprasPorEstado = new List<EstadoReporteViewModel>();
            Comprobantes = new List<ComprobanteInterfazViewModel>();
            NotasCredito = new List<NotaCreditoInterfazViewModel>();
            RevisionesOperativas = new List<RevisionOperativaViewModel>();
        }

        public string Origen { get; set; }
        public string Destino { get; set; }
        public DateTime? FechaViaje { get; set; }
        public int CantidadPasajeros { get; set; }
        public int? VueloId { get; set; }
        public string ModeloAeronave { get; set; }
        public bool EsItinerario { get; set; }
        public int? OrdenTramo { get; set; }
        public int TotalTramos { get; set; }
        public int? ReservaId { get; set;}
        public string CodigoReserva { get; set; }
        public string EstadoReserva { get; set; }
        public string EstadoCompra { get; set; }
        public string EstadoPago { get; set; }
        public string Mensaje { get; set; }
        public string CodigoReservaBusqueda { get; set; }
        public string DocumentoBusqueda { get; set; }
        public decimal Total { get; set; }
        public string Moneda { get; set; }
        public IList<SelectListItem> Aeropuertos { get; set; }
        public IList<SelectListItem> Destinos { get; set; }
        public IList<SelectListItem> TiposDocumento { get; set; }
        public IList<VueloTarjetaViewModel> Vuelos { get; set;}
        public IList<ItinerarioTarjetaViewModel> Itinerarios { get; set; }
        public IList<AsientoInterfazViewModel> Asientos { get; set; }
        public IList<PasajeroInterfazViewModel> Pasajeros { get; set; }
        public IList<ReservaInterfazViewModel> Reservas { get; set; }
        public IList<TramoInterfazViewModel> Tramos { get; set; }
        public IList<AccesoAdministrativoViewModel> AccesosAdministrativos { get; set; }
        public IList<UsuarioAdministrativoViewModel> UsuariosAdministrativos { get; set; }
        public IList<RutaOperacionViewModel> RutasOperacion { get; set; }
        public int TotalVuelos { get; set;}
        public int TotalAeronaves { get; set; }
        public int TotalReservas { get; set; }
        public int TotalPasajeros { get; set; }
        public int TotalCompras { get; set; }
        public int TotalComprobantes { get; set; }
        public DateTime? ReporteDesde { get; set; }
        public DateTime? ReporteHasta { get; set; }
        public string ReporteAviso { get; set; }
        public IList<EstadoReporteViewModel> ReservasPorEstado { get; set; }
        public IList<EstadoReporteViewModel> ComprasPorEstado { get; set; }
        public IList<ComprobanteInterfazViewModel> Comprobantes { get; set; }
        public IList<NotaCreditoInterfazViewModel> NotasCredito { get; set; }
        public IList<RevisionOperativaViewModel> RevisionesOperativas { get; set; }
    }

    public class RutaOperacionViewModel
    {
        public string Ruta { get; set; }
        public string DiasHorarios { get; set; }
    }

    public class VueloTarjetaViewModel
    {
        public int VueloId { get; set; }
        public string CodigoVuelo { get; set; }
        public string Origen { get; set; }
        public string Destino { get; set; }
        public DateTime Fecha { get; set; }
        public string HoraSalida { get; set; }
        public string HoraLlegada { get; set; }
        public string Matricula { get; set; }
        public string Estado { get; set; }
        public int AsientosDisponibles { get; set; }
        public decimal Tarifa { get; set; }
        public string Moneda { get; set; }
    }

    public class ItinerarioTarjetaViewModel
    {
        public int ItinerarioId { get; set; }
        public string Referencia { get; set; }
        public string Ruta { get; set; }
        public DateTime Fecha { get; set; }
        public string Horario { get; set; }
        public int AsientosDisponibles { get; set; }
        public decimal TarifaTotal { get; set; }
        public string Moneda { get; set; }
        public IList<TramoReservaViewModel> Tramos { get; set; }
    }

    public class AsientoInterfazViewModel
    {
        public byte Numero { get; set; }
        public byte Fila { get; set; }
        public string Codigo { get; set;}
        public string Ubicacion { get; set; }
        public string Estado { get; set; }
        public string EstadoVisual { get; set; }
        public bool Seleccionable { get; set; }
        public bool Seleccionado { get; set; }
    }

    public class PasajeroInterfazViewModel
    {
        public int Numero { get; set; }

        [Required(ErrorMessage = "Ingrese los nombres del pasajero.")]
        [StringLength(100, MinimumLength = 1)]
        public string Nombres { get; set; }

        [Required(ErrorMessage = "Ingrese los apellidos del pasajero.")]
        [StringLength(100, MinimumLength = 1)]
        public string Apellidos { get; set; }

        [Required(ErrorMessage = "Seleccione un tipo de documento.")]
        public string TipoDocumento { get; set; }

        [Required(ErrorMessage = "Ingrese el numero de documento.")]
        [StringLength(20, MinimumLength = 5)]
        public string Documento { get; set; }

        public string NombreCompleto { get; set; }
        public string Asiento { get; set; }
        public IList<string> AsientosPorTramo { get; set; }
        public string Ruta { get; set;}
        public string Vuelo { get; set; }
        public string EstadoCheckIn { get; set; }
    }

    public class ReservaInterfazViewModel
    {
        public int ReservaId { get; set; }
        public string CodigoReserva { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaViaje { get; set; }
        public string Ruta { get; set; }
        public int CantidadPasajeros { get; set; }
        public string EstadoReserva { get; set; }
        public string EstadoCompra { get; set; }
        public bool PermitePago { get; set; }
        // CAMBIO: muestra las acciones permitidas según el estado persistido de la reserva.
        public bool PermiteCancelar { get; set; }
        public bool EsCompraPagada { get; set; }
        public string PoliticaCancelacion { get; set; }
        public decimal Importe { get; set;}
        public string Moneda { get; set; }
        public IList<TramoInterfazViewModel> Tramos { get; set; }
    }

    public class TramoInterfazViewModel
    {
        public int Orden { get; set; }
        public string CodigoVuelo { get; set; }
        public string Ruta { get; set; }
        public DateTime Fecha { get; set; }
        public string Horario { get; set; }
        public string NombrePasajero { get; set; }
        public string Asiento { get; set; }
    }

    public class AccesoAdministrativoViewModel
    {
        public string Titulo { get; set; }
        public string Descripcion { get; set; }
        public string Controlador { get; set; }
        public string Accion { get; set; }
        public string Icono { get; set; }
    }

    public class UsuarioAdministrativoViewModel
    {
        public string Id { get; set; }
        public string Nombre { get; set; }
        public string Correo { get; set; }
        public string Roles { get; set; }
        public bool EsEditable { get; set; }
    }

    public class AdministracionPasajerosViewModel
    {
        public AdministracionPasajerosViewModel()
        {
            TiposDocumento = new List<SelectListItem>();
            Pasajeros = new List<PasajeroAdministracionItemViewModel>();
        }

        public string Busqueda { get; set; }
        public string Aviso { get; set; }
        public PasajeroAdministracionFormularioViewModel Formulario { get; set; }
        public IList<SelectListItem> TiposDocumento { get; set; }
        public IList<PasajeroAdministracionItemViewModel> Pasajeros { get; set; }
    }

    public class PasajeroAdministracionItemViewModel
    {
        public int PasajeroId { get; set; }
        public string TipoDocumento { get; set; }
        public string NumeroDocumento { get; set; }
        public string NombreCompleto { get; set; }
        public string Correo { get; set; }
        public int ReservasAsociadas { get; set; }
    }

    public class EstadoReporteViewModel
    {
        public string Estado { get; set; }
        public int Total { get; set; }
    }

    public class ComprobanteInterfazViewModel
    {
        public int ComprobanteId { get; set; }
        public string NumeroComprobante { get; set; }
        public string TipoComprobanteCodigo { get; set; }
        public string TipoComprobante { get; set; }
        public string Estado { get; set; }
        public DateTime FechaEmision { get; set; }
        public string CodigoReserva { get; set; }
        public decimal TotalOperacion { get; set; }
        public string MonedaCodigo { get; set; }
        public string EmisorNombre { get; set; }
        public string EmisorIdentificacion { get; set; }
        public string EmisorDireccion { get; set; }
        public string LeyendaComprobante { get; set; }
        public string MetodoPago { get; set; }
        public string MarcaTarjeta { get; set; }
        public string TarjetaEnmascarada { get; set; }
        public string CodigoOperacion { get; set; }
        public bool? EsSimulado { get; set; }
    }

    public class NotaCreditoInterfazViewModel
    {
        public string NumeroComprobante { get; set; }
        public string CodigoReserva { get; set; }
        public DateTime FechaEmision { get; set; }
        public decimal ImporteReembolsado { get; set; }
        public int PorcentajeReembolso { get; set; }
        public string Leyenda { get; set; }
    }

    public class RevisionOperativaViewModel
    {
        public int RevisionOperativaId { get; set; }
        public int VueloId { get; set; }
        public int? ItinerarioId { get; set; }
        public string ReferenciaItinerario { get; set; }
        public string CodigoReserva { get; set; }
        public string Motivo { get; set; }
        public DateTime FechaRegistro { get; set; }
    }

    public class PasajeroAdministracionFormularioViewModel
    {
        public int? PasajeroId { get; set; }

        [Required(ErrorMessage = "Seleccione un tipo de documento.")]
        [StringLength(20)]
        public string TipoDocumentoCodigo { get; set; }

        [Required(ErrorMessage = "Ingrese el numero de documento.")]
        [StringLength(20, MinimumLength = 5)]
        public string NumeroDocumento { get; set; }

        [Required(ErrorMessage = "Ingrese los nombres.")]
        [StringLength(100, MinimumLength = 1)]
        public string Nombres { get; set; }

        [Required(ErrorMessage = "Ingrese los apellidos.")]
        [StringLength(100, MinimumLength = 1)]
        public string Apellidos { get; set; }

        [DataType(DataType.Date)]
        public DateTime? FechaNacimiento { get; set; }

        [EmailAddress(ErrorMessage = "Ingrese un correo valido.")]
        [StringLength(256)]
        public string Correo { get; set; }

        [RegularExpression(@"^[0-9+ ]*$", ErrorMessage = "El telefono solo admite digitos, espacios y el signo +.")]
        [StringLength(20)]
        public string Telefono { get; set; }
    }

    }
