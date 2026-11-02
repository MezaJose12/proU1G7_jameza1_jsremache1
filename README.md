# Aerolinea VIP - guia de uso

Aplicacion web academica de reservas de vuelos, seleccion de asientos, compra/pago simulado y modulos de consulta administrativa. Usa ASP.NET MVC 5, ASP.NET Identity, Entity Framework 6 y SQL Server.

> **Importante:** es una demostracion. No procesa cobros, no uses tarjetas ni datos personales reales. Las compras y los comprobantes generados por el flujo de pago son registros academicos; no tienen validez tributaria ni comercial.

## 1. Requisitos y puesta en marcha

- Visual Studio con la carga de trabajo **ASP.NET y desarrollo web** y soporte para .NET Framework 4.8.1.
- SQL Server 2016 o posterior y SQL Server Management Studio (SSMS).
- NuGet para restaurar los paquetes del proyecto.

Pasos para ejecutar localmente:

1. Abre `src\proU1G7_jameza1_jsremache1.slnx` en Visual Studio.
2. En SSMS, abre `database\Script_BD_AerolineaVIP.sql` y ejecútalo completo en una instancia local. Crea `AerolineaVIP` (datos funcionales) y `AerolineaVIP_Identity` (usuarios/roles). Después ejecuta `database\Migracion_PoliticasAprobadas.sql` para instalar la cola DP-10 `RevisionOperativa`, que inactiva itinerarios afectados por vuelos cancelados y permite revisar incidencias desde el panel administrativo. En una base de dominio existente, ejecuta primero `database\Migracion_SolicitudCancelacion.sql` y luego la migración de políticas; ambas son repetibles y preservan las operaciones existentes. No vuelvas a ejecutar el script principal sobre una base con datos que quieras conservar.
   - **Advertencia:** la parte de dominio del script principal elimina y vuelve a crear sus objetos y datos. No lo vuelvas a ejecutar sobre una base con informacion que quieras conservar. La parte de Identity conserva las cuentas existentes.
3. Revisa `DefaultConnection` y `AerolineaVIPEntities` en `src\proU1G7_jameza1_jsremache1\Web.config`. Ambas usan autenticacion integrada; cambia `Data Source` si tu instancia no es local. La identidad de Windows que ejecuta IIS Express necesita acceso a las dos bases.
4. Para crear las cuentas iniciales, completa localmente las claves `InitialSuperUserUserName`, `InitialSuperUserEmail`, `InitialSuperUserPassword` y, si hace falta, las equivalentes `InitialAdministrator...`. En blanco no se crean cuentas; no guardes contrasenas reales en Git.
5. Restaura los paquetes NuGet si Visual Studio lo solicita.
6. Selecciona el proyecto web `proU1G7_jameza1_jsremache1` como proyecto de inicio y ejecuta con IIS Express (`F5`). La URL inicial abre la pagina principal.

## 2. Cuentas de prueba e inicio de sesion

`Global.asax.cs` crea las cuentas iniciales solo cuando las tres claves locales de usuario, correo y contrasena estan configuradas en `Web.config`. No hay credenciales predeterminadas en el codigo ni en el repositorio. Al iniciar, las cuentas configuradas reciben sus roles correspondientes; si una cuenta ya existe, se conserva su contrasena.

En **Iniciar sesion** puedes ingresar el nombre de usuario o el correo junto con la contrasena. Los intentos fallidos pueden bloquear temporalmente la cuenta. El rol publico es `Cliente`; solo un `SuperUsuario` puede crear cuentas con roles adicionales, incluido `Counter`.

### Crear una cuenta nueva

1. Selecciona **Registrarse / Crear cuenta** desde la barra superior o desde la pantalla de inicio de sesion.
2. Completa nombre de usuario, correo, contrasena y confirmacion.
3. El registro publico asigna automaticamente el rol `Cliente` e inicia la sesion al completarse.
4. La contrasena debe tener al menos 6 caracteres, mayuscula, minuscula, digito y caracter no alfanumerico.
5. Solo una cuenta autenticada con rol `SuperUsuario` puede ver y elegir un rol en el formulario de registro. No uses el registro publico para otorgarte privilegios.

La recuperacion/cambio por correo requiere configurar un servicio de correo; el servicio de envio del proyecto es una plantilla sin proveedor real.

## 3. Datos demo

El seeder sintetico solo corre en compilacion `Debug` cuando configuras expresamente `EnableDemoData=true`; por defecto esta desactivado. No habilites estos vuelos de demostracion contra una base con operaciones reales. Desactivar el seeder no borra los datos ya insertados.

Segun confirmacion del equipo, DP-01 a DP-13 estan aprobadas. El sistema aplica vuelos sin aeronave no comercializables y protegidos como PROGRAMADO, validacion de conexiones y de intervalos de aeronave, tarifas calculadas en servidor, reservas temporales, pagos academicos, cancelacion DP-08, itinerarios Galapagos completos, check-in y permisos Counter. DP-10 inactiva itinerarios afectados y registra una cola administrativa al cancelar un vuelo. Las tarifas, asignaciones iniciales de aeronaves y datos del emisor siguen siendo datos de demostracion; no habilitan operaciones reales. En DP-05 se usa el emisor academico `Aerolinea VIP - Proyecto academico`, con identificacion `NO APLICA - EMISOR ACADEMICO`; no se inventa una identificacion tributaria. Los comprobantes se muestran en pantalla, pero no generan un PDF descargable. La retencion y ventana de check-in se parametrizan en `Web.config` con `ReservaRetencionMinutos` (30), `CheckInAperturaHoras` (24) y `CheckInCierreMinutos` (60). DP-14 (RPO/RTO) y DP-15 (normativa) permanecen fuera del alcance.

## 4. Recorrido de las pantallas / wireframes

Las pantallas publicas de busqueda se pueden visitar sin cuenta. Para guardar una reserva hay que iniciar sesion. La barra superior ofrece Inicio, Buscar vuelos y, cuando ya iniciaste sesion, Mis reservas, Check-in y Comprobantes.

### Compra de un vuelo

1. **Pantalla 5 - Buscar vuelos** (`/Vuelos/Buscar`): elige origen, destino, fecha y de 1 a 7 pasajeros; pulsa **Buscar**. Los destinos disponibles dependen de rutas activas.
2. **Pantalla 6 - Resultados** (`/Vuelos/Resultados`): compara horario, tarifa, estado y asientos disponibles. Puedes revisar disponibilidad o continuar con **Seleccionar vuelo**. Si no has iniciado sesion, el sistema te lleva a iniciar sesion y luego regresa al vuelo.
3. **Pantalla 7 - Disponibilidad de asientos** (`/Vuelos/Disponibilidad/{id}`): consulta el mapa/estado de los asientos sin reservarlos.
4. **Pantalla 8 - Seleccion de asientos** (`/Reservas/SeleccionAsiento/{id}`): elige un asiento disponible por pasajero y pulsa **Continuar a pasajeros**. La seleccion guardada en la sesion es temporal; el servidor vuelve a verificar disponibilidad al confirmar la reserva.
5. **Pantalla 9 - Datos de pasajeros** (`/Reservas/Pasajeros`): completa tipo y numero de documento, nombres y apellidos de cada pasajero; continua a la revision.
6. **Pantalla 10 - Revision y confirmacion de reserva** (`/Reservas/Confirmar`): revisa vuelo, pasajeros, asientos, importe y condiciones. Acepta las condiciones y elige **Confirmar reserva** para guardar una reserva pendiente o **Confirmar y pagar** para ir al pago. La retencion se configura en minutos y nunca supera la salida del vuelo.
7. **Pantalla 11 - Pago simulado** (`/Compras/Pagar/{id}`): selecciona metodo y marca, completa datos ficticios validos y pulsa **Revisar y pagar**. En el modal, pulsa **Pagar** para registrar el intento. Una tarjeta de prueba cuyo numero termina en `0000` se rechaza; otro final de prueba permite simular la aprobacion. No se envia una autorizacion bancaria ni se guardan numero completo, CVV o vencimiento.
8. **Pantalla 12 - Estado/confirmacion** (`/Compras/Confirmacion/{id}`): consulta el estado de la reserva y compra.

### Area personal y servicio al pasajero

- **Pantalla 13 - Mis reservas** (`/Reservas/MisReservas`): lista las operaciones del cliente; los roles operativos autorizados pueden consultar reservas para asistir. Las pendientes permiten volver al pago mientras no venzan.
- **Pantalla 14 - Gestionar reserva** (`/Reservas/Gestionar/{id}`): consulta itinerario, pasajeros, asientos y estados. Una reserva pendiente puede cancelarse y libera sus asientos. Para una compra pagada, DP-08 permite cancelación completa solo si se solicita con al menos 24 horas antes del primer tramo y no existe check-in; se registra una nota de crédito académica por 100 % con más de 48 horas o 50 % entre 24 y 48 horas. No se realizan reembolsos monetarios reales.
- **Pantalla 15 - Check-in** (`/CheckIn/Index`): registra check-in por codigo de reserva y documento del pasajero, solo para compra pagada y dentro de la ventana parametrizada. Es un registro academico, no emite una tarjeta de embarque.
- **Pantalla 16 - Comprobantes** (`/Comprobantes/Index`): consulta comprobantes de venta académicos y notas de crédito simuladas asociados a compras. Son ficticios y no tienen validez tributaria; la descarga PDF pendiente de DP-05 aún no está disponible.

### Administracion

Los usuarios con rol `Administrador` o `SuperUsuario` acceden a **Administración** desde la barra o `/Admin/Index`. `Counter` no accede al panel ni a la programación administrativa.

- **Pantalla 17 - Panel administrativo** (`/Admin/Index`): resumen de vuelos, aeronaves, reservas y pasajeros.
- **Pantalla 18 - Vuelos y rutas** (`/Admin/Vuelos`): accesos a vuelos, frecuencias, rutas, itinerarios y aeropuertos.
- **Pantalla 19 - Flota y asientos** (`/Admin/Aeronaves`): accesos a aeronaves, modelos y asientos.
- **Pantalla 20 - Gestion operativa** (`/Admin/Gestion`): acceso al catálogo de documentos, consulta de reservas y administración de pasajeros.
- **Revisiones operativas** (`/Interfaces/AdminRevisiones`): vuelos cancelados, itinerarios inactivos y reservas afectadas pendientes de revisión DP-10.
- **Pasajeros** (`/Admin/Pasajeros`): disponible para `Administrador` y `SuperUsuario`; permite buscar, registrar y corregir datos personales, incluyendo los vinculados a reservas. No permite borrar registros para preservar el historial y las referencias de las operaciones.
- **Usuarios y roles** (`/Interfaces/AdminUsuarios`): solo `SuperUsuario` puede asignar los roles operativos `Cliente` y `Counter`; las cuentas administrativas y la propia cuenta quedan protegidas. El cambio invalida el sello de seguridad de Identity y requiere renovar la sesión.
- **Pantalla 21 - Reportes** (`/Admin/Reportes`): muestra conteos generales de registros y agrupaciones de reservas/compras por estado, con filtro de fechas de creación. No es un reporte financiero ni exportable.

Los controladores CRUD estan protegidos para `Administrador` y `SuperUsuario`; algunas operaciones destructivas, como eliminar aeronaves, quedan limitadas a `SuperUsuario`. El rol `Counter` puede asistir en los flujos operativos habilitados, sin acceso a la programacion administrativa.

## 5. Comportamiento del pago de demostracion

Cada intento persiste un pago simulado: los rechazos quedan como `RECHAZADO` y la compra puede reintentarse; un aprobado cambia la compra a `PAGADA`, confirma asientos y reserva, y emite un comprobante academico. No es una transaccion real ni acredita el pago de un servicio. No introduzcas datos reales de tarjeta. Para probar el rechazo, usa un numero ficticio que termine en `0000`; para aprobar, usa otro final. La validacion exige solo digitos y la longitud definida para la marca seleccionada, CVV con longitud configurada y una fecha vigente en formato `MM/AA`.

## 6. Rutas principales

| Funcion | Ruta |
| --- | --- |
| Inicio de sesion | `/Account/Login` |
| Registro | `/Account/Register` |
| Buscar vuelos | `/Vuelos/Buscar` |
| Mis reservas | `/Reservas/MisReservas` |
| Check-in | `/CheckIn/Index` |
| Comprobantes | `/Comprobantes/Index` |
| Administracion | `/Admin/Index` |

Las rutas de detalle y pasos de compra reciben identificadores, por ejemplo `/Reservas/SeleccionAsiento/123` o `/Compras/Pagar/123`; usa preferentemente los botones y enlaces de la aplicacion para mantener la reserva seleccionada.

# 7 . Funcionalidades puestas