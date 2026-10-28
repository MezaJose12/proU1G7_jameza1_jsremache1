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
            Asientos = new List<AsientoInterfazViewModel>();
            Pasajeros = new List<PasajeroInterfazViewModel>();
            Reservas = new List<ReservaInterfazViewModel>();
            Tramos = new List<TramoInterfazViewModel>();
            AccesosAdministrativos = new List<AccesoAdministrativoViewModel>();
        }

        public string Origen { get; set; }
        public string Destino { get; set; }
        public DateTime? FechaViaje { get; set; }
        public int CantidadPasajeros { get; set; }
        public int? VueloId { get; set; }
        public string ModeloAeronave { get; set; }
        public int? ReservaId { get; set;}
        public string CodigoReserva { get; set; }
        public string EstadoReserva { get; set; }
        public string EstadoCompra { get; set; }
        public string EstadoPago { get; set; }
        public string Mensaje { get; set; }
        public decimal Total { get; set; }
        public string Moneda { get; set; }
        public IList<SelectListItem> Aeropuertos { get; set; }
        public IList<SelectListItem> Destinos { get; set; }
        public IList<SelectListItem> TiposDocumento { get; set; }
        public IList<VueloTarjetaViewModel> Vuelos { get; set;}
        public IList<AsientoInterfazViewModel> Asientos { get; set; }
        public IList<PasajeroInterfazViewModel> Pasajeros { get; set; }
        public IList<ReservaInterfazViewModel> Reservas { get; set; }
        public IList<TramoInterfazViewModel> Tramos { get; set; }
        public IList<AccesoAdministrativoViewModel> AccesosAdministrativos { get; set; }
        public int TotalVuelos { get; set; }
        public int TotalAeronaves { get; set; }
        public int TotalReservas { get; set; }
        public int TotalPasajeros { get; set; }
        public int TotalCompras { get; set; }
        public int TotalComprobantes { get; set; }
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

        [Required(ErrorMessage = "Ingrese el número de documento.")]
        [StringLength(20, MinimumLength = 5)]
        public string Documento { get; set; }

        public string NombreCompleto { get; set; }
        public string Asiento { get; set; }
        public string Ruta { get; set; }
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
}
