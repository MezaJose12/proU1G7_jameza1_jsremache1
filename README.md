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

En **Iniciar sesion** puedes ingresar el nombre de usuario o el correo junto con la contrasena. Los intentos fallidos pueden bloquear temporalmente la cuenta. El registro publico crea cuentas con rol `Cliente`. Una cuenta autenticada con rol `Administrador` puede crear usuarios y asignarles un rol; `SuperUsuario` no puede crear usuarios ni asignar roles.

### Crear una cuenta nueva

1. Selecciona **Registrarse / Crear cuenta** desde la barra superior o desde la pantalla de inicio de sesion.
2. Completa nombre de usuario, correo, contrasena y confirmacion.
3. El registro publico asigna automaticamente el rol `Cliente` e inicia la sesion al completarse.
4. La contrasena debe tener al menos 8 caracteres; la configuracion de Identity tambien exige mayuscula, minuscula, digito y caracter no alfanumerico.
5. Solo una cuenta autenticada con rol `Administrador` puede ver y elegir un rol en el formulario de registro. `SuperUsuario` y `Counter` no pueden crear usuarios. No uses el registro publico para otorgarte privilegios.

La recuperacion/cambio por correo requiere configurar un servicio de correo; el servicio de envio del proyecto es una plantilla sin proveedor real.

## 3. Datos demo

El seeder sintetico solo corre en compilacion `Debug` cuando configuras expresamente `EnableDemoData=true`; por defecto esta desactivado. No habilites estos vuelos de demostracion contra una base con operaciones reales. Desactivar el seeder no borra los datos ya insertados.

Segun confirmacion del equipo, DP-01 a DP-13 estan aprobadas. El sistema aplica vuelos sin aeronave no comercializables y protegidos como PROGRAMADO, validacion de conexiones y de intervalos de aeronave, tarifas calculadas en servidor, reservas temporales, pagos academicos, cancelacion DP-08, itinerarios Galapagos completos, check-in y permisos Counter. DP-10 inactiva itinerarios afectados y registra una cola administrativa al cancelar un vuelo. Las tarifas, asignaciones iniciales de aeronaves y datos del emisor siguen siendo datos de demostracion; no habilitan operaciones reales. En DP-05 se usa el emisor academico `Aerolinea VIP - Proyecto academico`, con identificacion `NO APLICA - EMISOR ACADEMICO`; no se inventa una identificacion tributaria. Los comprobantes no generan un archivo PDF propio, pero pueden imprimirse o guardarse como PDF desde el dialogo de impresion del navegador. La retencion y ventana de check-in se parametrizan en `Web.config` con `ReservaRetencionMinutos` (30), `CheckInAperturaHoras` (24) y `CheckInCierreMinutos` (60). DP-14 (RPO/RTO) y DP-15 (normativa) permanecen fuera del alcance.

## 4. Recorrido de las pantallas / wireframes

Las pantallas publicas de busqueda se pueden visitar sin cuenta. Para seleccionar asientos, reservar o pagar debes iniciar sesion como `Cliente` o recibir asistencia de `Counter`. La barra de navegacion cambia segun el rol: los clientes ven sus reservas, check-in y comprobantes; el personal operativo ve los accesos administrativos permitidos y Counter tambien ve Check-in.

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
- **Pantalla 15 - Check-in** (`/CheckIn/Index`): registra el check-in por codigo de reserva y documento del pasajero, solo para compra pagada y dentro de la ventana parametrizada. Al completarlo, aparece la opcion **Ver e imprimir pases**; los pases son academicos y no sustituyen los controles aeroportuarios.
- **Pantalla 16 - Comprobantes** (`/Comprobantes/Index`): muestra un comprobante de venta por compra pagada y las notas de credito por separado. Las confirmaciones de reserva no se listan para evitar duplicar la informacion. Cada comprobante de venta tiene el boton **Ver e imprimir**; desde el navegador se puede imprimir o guardar como PDF. Se muestra el metodo de pago y solo los ultimos cuatro digitos de la tarjeta.

### Administracion

Los usuarios con rol `Administrador`, `SuperUsuario` o `Counter` acceden a **Administracion** desde la barra o `/Admin/Index`. Los modulos y acciones disponibles dependen del rol; Counter no puede programar rutas, frecuencias ni vuelos.

- **Pantalla 17 - Panel administrativo** (`/Admin/Index`): resumen de vuelos, aeronaves, reservas y pasajeros.
- **Pantalla 18 - Vuelos y rutas** (`/Admin/Vuelos`): accesos a vuelos, frecuencias, rutas, itinerarios y aeropuertos.
- **Pantalla 19 - Flota y asientos** (`/Admin/Aeronaves`): accesos a aeronaves, modelos y asientos.
- **Pantalla 20 - Gestion operativa** (`/Admin/Gestion`): acceso al catálogo de documentos, consulta de reservas y administración de pasajeros.
- **Revisiones operativas** (`/Interfaces/AdminRevisiones`): vuelos cancelados, itinerarios inactivos y reservas afectadas pendientes de revisión DP-10.
- **Pasajeros** (`/Admin/Pasajeros`): el personal autorizado puede buscar y corregir datos personales vinculados a reservas. Administrador y Counter pueden registrar pasajeros; no se permite borrar registros para preservar el historial.
- **Usuarios y roles** (`/Interfaces/AdminUsuarios`): `Administrador` puede crear usuarios y asignarles roles. `SuperUsuario` y `Counter` solo consultan las cuentas. El cambio de rol obliga al usuario afectado a iniciar sesion nuevamente.
- **Pantalla 21 - Reportes** (`/Admin/Reportes`): muestra conteos generales de registros y graficos de reservas/compras por estado, con filtro de fechas de creacion. Administrador puede imprimir o guardar el reporte como PDF desde el navegador; SuperUsuario puede consultarlo, pero no dispone del boton de impresion. No es un reporte financiero ni un comprobante tributario.

`Administrador` puede crear usuarios, asignar roles y administrar los registros autorizados. `SuperUsuario` puede consultar y editar los CRUD, pero no crear, eliminar ni desactivar registros; tampoco puede crear usuarios, asignar roles o imprimir reportes. `Counter` no administra la programacion de rutas ni vuelos, pero puede consultar vuelos y reservas, gestionar pasajeros, ver comprobantes, asistir una reserva asociandola a una cuenta Cliente y registrar check-in.

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

# 7. Guia de funcionamiento, roles y recomendaciones

## Permisos por tipo de usuario

| Rol | Puede hacer | Restricciones principales |
| --- | --- | --- |
| Visitante | Buscar vuelos y consultar disponibilidad de asientos. | No puede seleccionar asientos ni iniciar reservas. |
| `Cliente` | Seleccionar asientos, reservar y pagar de forma simulada; consultar sus reservas y comprobantes; hacer check-in e imprimir sus pases. | Solo consulta y gestiona sus propias operaciones. |
| `Administrador` | Administrar usuarios y roles, catalogos, rutas, frecuencias, vuelos, flota, pasajeros, reservas y reportes; imprimir reportes. | Debe mantener una cuenta personal distinta de las cuentas de prueba y no compartir credenciales. |
| `SuperUsuario` | Consultar y editar informacion administrativa; consultar vuelos, reservas, reportes y revisiones operativas. | No puede crear usuarios, asignar roles, crear/eliminar/desactivar registros ni imprimir reportes. |
| `Counter` | Buscar disponibilidad, consultar vuelos y reservas desde Administracion, ver comprobantes, gestionar pasajeros, reservar para un Cliente y registrar check-in/imprimir pases. | No tiene acceso a Mis reservas ni a la programacion de rutas y vuelos. No puede cancelar compras pagadas del cliente. |

## Preparar y programar la operacion

Configura los datos en este orden para que las pantallas posteriores tengan sus dependencias:

1. **Flota:** crea primero el modelo con fabricante, nombre y capacidad; configura el mapa de asientos del modelo; luego registra la aeronave real con su matricula (identificador unico, por ejemplo `AV-01`). La matricula no es el modelo ni el codigo de vuelo.
2. **Aeropuertos:** registra los aeropuertos con codigo IATA de tres letras y localidad.
3. **Rutas:** conecta un aeropuerto de origen con uno de destino y establece la tarifa referencial por pasajero y tramo.
4. **Frecuencias:** define para cada ruta un dia de la semana y las horas locales recurrentes. La frecuencia es una plantilla, no un vuelo de una fecha concreta.
5. **Vuelos:** en Vuelos programados, selecciona el rango de fechas y genera las ocurrencias de las frecuencias activas cuyo dia semanal coincide con cada fecha. El rango es inclusivo y admite hasta 366 dias. La generacion no asigna aeronaves automaticamente: asigna y verifica una aeronave disponible a cada vuelo antes de ofrecerlo para reserva. Revisa la lista resultante y evita regenerar sin necesidad.
6. **Itinerarios:** crea el itinerario y agrega vuelos ya programados en orden de conexion. Verifica que el destino de cada tramo coincida con el origen del siguiente y que los horarios permitan la conexion.
7. **Verificacion:** usa Buscar vuelos con una fecha futura para comprobar que la ruta, frecuencia, vuelo, aeronave y asientos aparecen disponibles antes de iniciar la venta.

Si no aparece un vuelo, revisa que aeropuerto y ruta esten activos, que la frecuencia cubra el dia consultado, que exista un vuelo generado para esa fecha, que tenga una aeronave asignada y que esta se encuentre disponible.

## Reserva asistida por Counter

1. Entra por **Administracion > Buscar vuelos** y selecciona ruta, fecha y cantidad de pasajeros.
2. Si ya conoces al cliente, selecciona su cuenta Cliente en la busqueda. Counter solo puede asociar la operacion a una cuenta Cliente; no se debe registrar una reserva asistida en la cuenta del operador.
3. Consulta los vuelos y disponibilidad. Para continuar, selecciona al cliente y luego el vuelo/itinerario y asientos.
4. Ingresa datos de prueba del pasajero, revisa la operacion y confirma la reserva. Si se elige pagar, completa el pago simulado con datos ficticios; no ingreses informacion personal real.
5. La reserva queda asociada al cliente seleccionado, por lo que este puede verla en **Mis reservas** y consultar los comprobantes al entrar con su propia cuenta. Counter consulta todas las reservas y comprobantes desde el panel administrativo.

## Asientos, compra y check-in

- En el mapa, **verde** significa disponible, **morado** reservado temporalmente y **rojo** ocupado/confirmado. Una seleccion de asientos durante la compra es temporal; los asientos se validan de nuevo al confirmar.
- Una reserva no equivale a una compra pagada. La reserva pendiente conserva los asientos solo durante el plazo indicado; completa el pago simulado antes de que venza.
- Para hacer check-in se necesita una compra pagada, el codigo de reserva y el documento del pasajero. La ventana se abre el numero de horas configurado antes del primer tramo y cierra los minutos configurados antes de la salida (por defecto, 24 horas y 60 minutos).
- Despues de completar el check-in, usa **Ver e imprimir pases** desde la reserva correspondiente. Puedes imprimirlos o guardarlos como PDF desde el dialogo del navegador. Son documentos de demostracion y no habilitan embarque real.

## Recomendaciones de seguridad y mantenimiento

- Utiliza solo datos sinteticos y numeros de tarjeta ficticios. La aplicacion no es una pasarela de pago y no debe recibir datos financieros ni personales reales.
- No guardes contrasenas, claves de configuracion ni credenciales de SQL Server en el repositorio. Usa configuracion local protegida y cuentas con permisos minimos.
- Antes de ejecutar un script o migracion SQL, crea una copia de seguridad. El script inicial puede eliminar y recrear objetos de la base de dominio; no lo ejecutes sobre datos que quieras conservar.
- Mantener las bases `AerolineaVIP` y `AerolineaVIP_Identity` configuradas correctamente en `Web.config`. Si la aplicacion no inicia o no carga datos, comprueba la instancia SQL, la cadena de conexion, el acceso de IIS Express y que se ejecutaron las migraciones indicadas en la seccion 1.
- Para validar un cambio de roles, cierra e inicia sesion con la cuenta afectada. Para probar permisos, utiliza una cuenta distinta para cada rol y evita que una cuenta de prueba acumule varios roles.
- Los comprobantes y reportes se imprimen desde el navegador. Revisa la vista previa, el rango de fechas y la impresora/destino antes de imprimir.

## Limitaciones conocidas

- Los pagos, comprobantes, notas de credito, reservas y pases son academicos; no tienen efecto financiero, tributario ni aeroportuario real.
- Los horarios son horas locales configuradas para la operacion del proyecto. Verifica fechas y conexiones al crear itinerarios.
- Marcar una revision operativa como revisada no resuelve automaticamente la causa de la incidencia.
- La opcion del navegador para imprimir puede usarse aunque la interfaz no muestre un boton; la restriccion de SuperUsuario se refiere al boton de impresion de reportes de la aplicacion.
