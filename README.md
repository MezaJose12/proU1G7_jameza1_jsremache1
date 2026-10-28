# Aerolinea VIP - guia de uso

Aplicacion web academica de reservas de vuelos, seleccion de asientos, compra/pago simulado y modulos de consulta administrativa. Usa ASP.NET MVC 5, ASP.NET Identity, Entity Framework 6 y SQL Server.

> **Importante:** es una demostracion. No procesa cobros, no uses tarjetas ni datos personales reales. Las compras y los comprobantes generados por el flujo de pago son registros academicos; no tienen validez tributaria ni comercial.

## 1. Requisitos y puesta en marcha

- Visual Studio con la carga de trabajo **ASP.NET y desarrollo web** y soporte para .NET Framework 4.8.1.
- SQL Server 2016 o posterior y SQL Server Management Studio (SSMS).
- NuGet para restaurar los paquetes del proyecto.

Pasos para ejecutar localmente:

1. Abre `src\proU1G7_jameza1_jsremache1.slnx` en Visual Studio.
2. En SSMS, abre `database\Script_BD_AerolineaVIP.sql` y ejecútalo completo en una instancia local. Crea `AerolineaVIP` (datos funcionales) y `AerolineaVIP_Identity` (usuarios/roles).
   - **Advertencia:** la parte de dominio del script elimina y vuelve a crear sus objetos y datos. No lo vuelvas a ejecutar sobre una base con informacion que quieras conservar. La parte de Identity conserva las cuentas existentes.
3. Revisa las cadenas `DefaultConnection` y `AerolineaVIPEntities` en `src\proU1G7_jameza1_jsremache1\Web.config`. Deben apuntar a la instancia SQL local y a las bases anteriores. Ajusta autenticacion/servidor a tu entorno; no guardes contrasenas de produccion en el repositorio.
4. Restaura los paquetes NuGet si Visual Studio lo solicita.
5. Selecciona el proyecto web `proU1G7_jameza1_jsremache1` como proyecto de inicio y ejecuta con IIS Express (`F5`). La URL inicial abre la pagina principal.

## 2. Cuentas de prueba e inicio de sesion

En `Global.asax.cs`, al iniciar la aplicacion se crean si aun no existen estas cuentas y se asignan sus roles:

| Usuario | Correo | Contrasena local inicial | Acceso |
| --- | --- | --- | --- |
| `SuperAdmin` | `jameza1@espe.edu.ec` | `P@ssw0rd` | SuperUsuario y permisos administrativos |
| `Administrador` | `jsremache1@espe.edu.ec` | `P@ssw0rd` | Administrador y permisos de consulta/edicion |

En **Iniciar sesion** puedes ingresar el nombre de usuario o el correo junto con la contrasena. Si la cuenta ya existia en `AerolineaVIP_Identity`, el arranque no cambia su contrasena: usa la contrasena vigente de esa base. Los intentos fallidos pueden bloquear temporalmente la cuenta.

Estas credenciales estan escritas en el codigo para desarrollo academico: no las uses en produccion ni publiques el sitio con ellas. Cambialas y protege las cadenas de conexion antes de compartir o desplegar el proyecto.

### Crear una cuenta nueva

1. Selecciona **Registrarse / Crear cuenta** desde la barra superior o desde la pantalla de inicio de sesion.
2. Completa nombre de usuario, correo, contrasena y confirmacion.
3. El registro publico asigna automaticamente el rol `Cliente` e inicia la sesion al completarse.
4. La contrasena debe tener al menos 6 caracteres, mayuscula, minuscula, digito y caracter no alfanumerico.
5. Solo una cuenta autenticada con rol `SuperUsuario` puede ver y elegir un rol en el formulario de registro. No uses el registro publico para otorgarte privilegios.

La recuperacion/cambio por correo requiere configurar un servicio de correo; el servicio de envio del proyecto es una plantilla sin proveedor real.

## 3. Datos demo

En compilacion `Debug`, `EnableDemoData=true` en `Web.config` habilita el seeder al arrancar. Inserta solamente datos faltantes para una ruta Quito–Guayaquil, vuelos de los proximos 14 dias, aeronave demo de siete asientos, catalogos y reservas de muestra asociadas a `SuperAdmin` (una confirmada y otra pendiente). La fecha debe estar dentro de las fechas que genero el seeder y debe existir una frecuencia para ese dia.

Las reservas nuevas quedan pendientes durante 15 minutos; despues de ese plazo los asientos dejan de estar retenidos. Para dejar de cargar datos, cambia `EnableDemoData` a `false`. `Web.Release.config` tambien lo desactiva al transformar para Release. Desactivar el seeder **no borra** los datos que ya insertó. No lo habilites contra una base de datos de produccion.

## 4. Recorrido de las pantallas / wireframes

Las pantallas publicas de busqueda se pueden visitar sin cuenta. Para guardar una reserva hay que iniciar sesion. La barra superior ofrece Inicio, Buscar vuelos y, cuando ya iniciaste sesion, Mis reservas, Check-in y Comprobantes.

### Compra de un vuelo

1. **Pantalla 5 - Buscar vuelos** (`/Vuelos/Buscar`): elige origen, destino, fecha y de 1 a 7 pasajeros; pulsa **Buscar**. Los destinos disponibles dependen de rutas activas.
2. **Pantalla 6 - Resultados** (`/Vuelos/Resultados`): compara horario, tarifa, estado y asientos disponibles. Puedes revisar disponibilidad o continuar con **Seleccionar vuelo**. Si no has iniciado sesion, el sistema te lleva a iniciar sesion y luego regresa al vuelo.
3. **Pantalla 7 - Disponibilidad de asientos** (`/Vuelos/Disponibilidad/{id}`): consulta el mapa/estado de los asientos sin reservarlos.
4. **Pantalla 8 - Seleccion de asientos** (`/Reservas/SeleccionAsiento/{id}`): elige un asiento disponible por pasajero y pulsa **Continuar a pasajeros**. La seleccion guardada en la sesion es temporal; el servidor vuelve a verificar disponibilidad al confirmar la reserva.
5. **Pantalla 9 - Datos de pasajeros** (`/Reservas/Pasajeros`): completa tipo y numero de documento, nombres y apellidos de cada pasajero; continua a la revision.
6. **Pantalla 10 - Revision y confirmacion de reserva** (`/Reservas/Confirmar`): revisa vuelo, pasajeros, asientos, importe y condiciones. Acepta las condiciones y elige **Confirmar reserva** para guardar una reserva pendiente o **Confirmar y pagar** para ir al pago. La reserva pendiente retiene los asientos por 15 minutos.
7. **Pantalla 11 - Pago simulado** (`/Compras/Pagar/{id}`): selecciona metodo y marca, completa datos ficticios validos y pulsa **Revisar y pagar**. En el modal, pulsa **Pagar** para registrar la compra/pago academico. La aplicacion no envia una autorizacion bancaria ni guarda el numero completo, CVV o fecha de vencimiento de la tarjeta.
8. **Pantalla 12 - Estado/confirmacion** (`/Compras/Confirmacion/{id}`): consulta el estado de la reserva y compra.

### Area personal y servicio al pasajero

- **Pantalla 13 - Mis reservas** (`/Reservas/MisReservas`): lista solo operaciones de la cuenta autenticada. Las pendientes permiten volver al pago mientras no venzan.
- **Pantalla 14 - Gestionar reserva** (`/Reservas/Gestionar/{id}`): consulta itinerario, pasajeros, asientos y estados. Cancelaciones, cambios y reembolsos no estan implementados.
- **Pantalla 15 - Check-in** (`/CheckIn/Index`): consulta reservas confirmadas. La apertura por ventana y la emision de tarjetas de embarque aun no tienen accion implementada.
- **Pantalla 16 - Comprobantes** (`/Comprobantes/Index`): consulta comprobantes simulados asociados a compras. Son ficticios y no tienen validez tributaria.

### Administracion

Los usuarios con permiso `view` acceden a **Administracion** desde la barra o `/Admin/Index`.

- **Pantalla 17 - Panel administrativo** (`/Admin/Index`): resumen de vuelos, aeronaves, reservas y pasajeros.
- **Pantalla 18 - Vuelos y rutas** (`/Admin/Vuelos`): accesos a vuelos, frecuencias, rutas, itinerarios y aeropuertos.
- **Pantalla 19 - Flota y asientos** (`/Admin/Aeronaves`): accesos a aeronaves, modelos y asientos.
- **Pantalla 20 - Gestion operativa** (`/Admin/Gestion`): accesos disponibles para catalogo de documentos; pasajeros, reservas y gestion generica de usuarios se muestran como pendientes de implementar.
- **Pantalla 21 - Reportes** (`/Admin/Reportes`): conteos basicos de registros; no son reportes financieros ni exportables.

Los modulos enlazados a controladores CRUD tienen permisos propios. `SuperAdmin` tiene los roles `SuperUsuario`, `view`, `create`, `edit` y `delete`; `Administrador` tiene `Administrador`, `view`, `create` y `edit`. Algunas operaciones administrativas, como eliminar aeronaves, estan limitadas a `SuperUsuario`.

## 5. Comportamiento del pago de demostracion

Al confirmar el pago se persisten una compra y un pago identificados como simulados, se confirma la reserva y se emite un comprobante academico. No es una transaccion real ni acredita el pago de un servicio. No introduzcas datos reales de tarjeta. Para datos de prueba, la validacion exige solo digitos y la longitud definida para la marca seleccionada, CVV con la longitud configurada y una fecha futura en formato `MM/AA`.

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