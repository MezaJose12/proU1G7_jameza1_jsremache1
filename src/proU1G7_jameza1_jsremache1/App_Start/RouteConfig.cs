using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace proU1G7_jameza1_jsremache1
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            // Rutas visibles del mapa de navegación del apartado 11.4.2.
            routes.MapRoute("FlightSearch", "Vuelos/Buscar", new { controller = "Interfaces", action = "Buscar" });
            routes.MapRoute("FlightResults", "Vuelos/Resultados", new { controller = "Interfaces", action = "Resultados" });
            routes.MapRoute("FlightAvailability", "Vuelos/Disponibilidad/{id}", new { controller = "Interfaces", action = "Disponibilidad" });
            routes.MapRoute("SeatSelection", "Reservas/SeleccionAsiento/{id}", new { controller = "Interfaces", action = "SeleccionarAsiento" });
            routes.MapRoute("ReservaPasajeros", "Reservas/Pasajeros", new { controller = "Interfaces", action = "Pasajeros" });
            routes.MapRoute("ReservaConfirmar", "Reservas/Confirmar", new { controller = "Interfaces", action = "Confirmar" });
            routes.MapRoute("PurchasePay", "Compras/Pagar/{id}", new { controller = "Interfaces", action = "Pagar" });
            routes.MapRoute("PurchaseConfirm", "Compras/Confirmacion/{id}", new { controller = "Interfaces", action = "Confirmacion" });
            routes.MapRoute("MyReservations", "Reservas/MisReservas", new { controller = "Interfaces", action = "MisReservas" });
            routes.MapRoute("ManageReservation", "Reservas/Gestionar/{id}", new { controller = "Interfaces", action = "Gestionar" });
            routes.MapRoute("CheckIn", "CheckIn/Index", new { controller = "Interfaces", action = "CheckIn" });
            routes.MapRoute("Receipts", "Comprobantes/Index", new { controller = "Interfaces", action = "Comprobantes" });
            routes.MapRoute("AdminHome", "Admin/Index", new { controller = "Interfaces", action = "AdminIndex" });
            routes.MapRoute("AdminFlights", "Admin/Vuelos", new { controller = "Interfaces", action = "AdminVuelos" });
            routes.MapRoute("AdminAircraft", "Admin/Aeronaves", new { controller = "Interfaces", action = "AdminAeronaves" });
            routes.MapRoute("AdminManagement", "Admin/Gestion", new { controller = "Interfaces", action = "AdminGestion" });
            routes.MapRoute("AdminReports", "Admin/Reportes", new { controller = "Interfaces", action = "AdminReportes" });

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}
