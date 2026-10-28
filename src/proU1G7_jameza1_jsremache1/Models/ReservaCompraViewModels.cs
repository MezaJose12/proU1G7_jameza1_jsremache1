using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace proU1G7_jameza1_jsremache1.Models
{
    // Datos de solo presentación para la pantalla 10; el controlador debe calcular importes
    // y recuperar itinerario, pasajeros y asientos desde la operación del servidor.
    public class ReservaConfirmacionViewModel
    {
        public ReservaConfirmacionViewModel()
        {
            Tramos = new List<TramoReservaViewModel>();
            Pasajeros = new List<PasajeroReservaViewModel>();
        }

        public IList<TramoReservaViewModel> Tramos { get; set; }
        public IList<PasajeroReservaViewModel> Pasajeros { get; set; }
        public decimal TotalEstimado { get; set; }
        public string MonedaCodigo { get; set; }
        public string CondicionesReserva { get; set; }

        [Display(Name = "Acepto las condiciones de la reserva")]
        public bool AceptaCondiciones { get; set; }
    }

    public class TramoReservaViewModel
    {
        public int Orden { get; set; }
        public string CodigoVuelo { get; set; }
        public string Ruta { get; set; }
        public DateTime Fecha { get; set; }
        public string Horario { get; set; }
    }

    public class PasajeroReservaViewModel
    {
        public string NombreCompleto { get; set; }
        public string TipoDocumento { get; set; }
        public string DocumentoEnmascarado { get; set; }
        public IList<string> AsientosPorTramo { get; set; }
        public decimal ImporteEstimado { get; set; }
    }

    // Modelo exclusivo de presentación para la simulación; no persiste ningún dato
    // de tarjeta, incluido el número completo, los últimos dígitos o el CVV.
    public class PagoSimuladoViewModel
    {
        public PagoSimuladoViewModel()
        {
            MetodosPago = new List<MetodoPagoOpcionViewModel>();
            MarcasTarjeta = new List<MarcaTarjetaOpcionViewModel>();
        }

        public int ReservaId { get; set; }
        public string CodigoReserva { get; set; }
        public string Ruta { get; set; }
        public DateTime FechaViaje { get; set; }
        public string Horario { get; set; }
        public int CantidadPasajeros { get; set; }
        public string Asientos { get; set; }
        public decimal Total { get; set; }
        public string MonedaCodigo { get; set; }
        public string EstadoCompra { get; set; }
        public IList<MetodoPagoOpcionViewModel> MetodosPago { get; set; }
        public IList<MarcaTarjetaOpcionViewModel> MarcasTarjeta { get; set; }

        [Required(ErrorMessage = "Seleccione un método de pago.")]
        [Display(Name = "Método de pago")]
        public string MetodoPagoCodigo { get; set; }

        [Required(ErrorMessage = "Seleccione la marca de su tarjeta.")]
        [Display(Name = "Marca de la tarjeta")]
        public string MarcaTarjetaCodigo { get; set; }

        [Required(ErrorMessage = "Ingrese el número de tarjeta.")]
        [RegularExpression(@"^[0-9 ]+$", ErrorMessage = "Use solo números en el número de tarjeta.")]
        [Display(Name = "Número de tarjeta")]
        public string NumeroTarjeta { get; set; }

        [Required(ErrorMessage = "Ingrese el nombre del titular.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 100 caracteres.")]
        [Display(Name = "Nombre del titular")]
        public string NombreTitular { get; set; }

        [Required(ErrorMessage = "Ingrese la fecha de vencimiento.")]
        [RegularExpression(@"^(0[1-9]|1[0-2])/[0-9]{2}$", ErrorMessage = "Use el formato MM/AA.")]
        [Display(Name = "Vencimiento")]
        public string Vencimiento { get; set; }

        [Required(ErrorMessage = "Ingrese el código de seguridad.")]
        [RegularExpression(@"^[0-9]{3,4}$", ErrorMessage = "El código debe tener 3 o 4 dígitos.")]
        [Display(Name = "Código de seguridad")]
        public string CodigoSeguridad { get; set; }
    }

    public class MetodoPagoOpcionViewModel
    {
        public string Codigo { get; set; }
        public string Nombre { get; set; }
    }

    public class MarcaTarjetaOpcionViewModel
    {
        public string Codigo { get; set; }
        public string Nombre { get; set; }
        public byte LongitudNumero { get; set; }
        public byte LongitudCodigoSeguridad { get; set; }
    }
}
