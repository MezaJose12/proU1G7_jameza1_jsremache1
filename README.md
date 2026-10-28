# proU1G7_jameza1_jsremache1

## Datos de demostración

La aplicación puede cargar registros sintéticos para revisar las pantallas de vuelos, asientos, reservas, pago simulado y administración. La carga se ejecuta al iniciar la aplicación únicamente en una compilación `Debug` cuando `EnableDemoData` está en `true` en `proU1G7_jameza1_jsremache1/Web.config`.

El seeder agrega solo datos faltantes: ruta Quito–Guayaquil, vuelos para los próximos 14 días, configuración de siete asientos, catálogos mínimos y dos reservas de demostración vinculadas a la cuenta `SuperAdmin` (una confirmada y otra pendiente). No crea pagos ni comprobantes reales. La pantalla de comprobantes conserva su estado vacío hasta que exista un comprobante.

Al confirmar una reserva desde el flujo de compra, la aplicación guarda la reserva, los pasajeros y los asientos en una transacción. Las nuevas reservas quedan pendientes durante 15 minutos; al vencer, sus asientos vuelven a estar disponibles. El pago es solo una simulación: no se realiza un cobro ni se guardan datos de tarjeta, compras o pagos.

Para desactivar los datos demo, cambia `EnableDemoData` a `false`. La transformación `Web.Release.config` lo desactiva en despliegues Release; no habilites el seeder contra una base de datos de producción. Los registros insertados no se eliminan automáticamente al desactivar la opción.