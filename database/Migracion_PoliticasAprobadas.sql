/*
 * Migración no destructiva para aplicar las políticas académicas aprobadas DP-03, DP-05, DP-08 y DP-10.
 * Ejecute en AerolineaVIP. No elimina reservas, pagos, comprobantes ni asignaciones existentes.
 */
USE AerolineaVIP;
GO
SET XACT_ABORT ON;
GO

IF OBJECT_ID(N'dbo.ParametroPolitica', N'U') IS NULL
BEGIN
	CREATE TABLE dbo.ParametroPolitica (
		Codigo VARCHAR(60) NOT NULL CONSTRAINT PK_ParametroPolitica PRIMARY KEY,
		Valor INT NOT NULL,
		CONSTRAINT CK_ParametroPolitica_Valor CHECK (Valor >= 0)
	);
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.ParametroPolitica WHERE Codigo = 'PreparacionAeronaveMinutos')
	INSERT INTO dbo.ParametroPolitica (Codigo, Valor) VALUES ('PreparacionAeronaveMinutos', 30);
IF NOT EXISTS (SELECT 1 FROM dbo.ParametroPolitica WHERE Codigo = 'LiberacionAeronaveMinutos')
	INSERT INTO dbo.ParametroPolitica (Codigo, Valor) VALUES ('LiberacionAeronaveMinutos', 30);
IF NOT EXISTS (SELECT 1 FROM dbo.ParametroPolitica WHERE Codigo = 'ConexionMinimaMinutos')
	INSERT INTO dbo.ParametroPolitica (Codigo, Valor) VALUES ('ConexionMinimaMinutos', 60);
GO

-- CAMBIO DP-02: genera ocurrencias semanales de frecuencias activas sin asignar aeronaves.
CREATE OR ALTER PROCEDURE dbo.usp_GenerarVuelos
	@FechaInicio DATE,
	@FechaFin DATE
AS
BEGIN
	SET NOCOUNT ON;

	IF @FechaInicio IS NULL OR @FechaFin IS NULL OR @FechaFin < @FechaInicio
		THROW 50051, N'Rango de fechas invalido.', 1;
	IF DATEDIFF(DAY, @FechaInicio, @FechaFin) > 365
		THROW 50052, N'El rango no puede superar 366 dias.', 1;

	;WITH Fechas AS
	(
		SELECT @FechaInicio AS Fecha
		UNION ALL
		SELECT DATEADD(DAY, 1, Fecha) FROM Fechas WHERE Fecha < @FechaFin
	)
	INSERT INTO dbo.Vuelo (FrecuenciaId, FechaOperacion)
	SELECT f.FrecuenciaId, fe.Fecha
	FROM Fechas fe
	JOIN dbo.Frecuencia f
		ON f.DiaSemana = (DATEDIFF(DAY, CAST('19000101' AS DATE), fe.Fecha) % 7) + 1
		AND f.Activa = 1
	JOIN dbo.Ruta r ON r.RutaId = f.RutaId AND r.Activa = 1
	WHERE NOT EXISTS
	(
		SELECT 1 FROM dbo.Vuelo v
		WHERE v.FrecuenciaId = f.FrecuenciaId AND v.FechaOperacion = fe.Fecha
	)
	OPTION (MAXRECURSION 400);
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.TipoComprobante WHERE Codigo = 'FACTURA')
	INSERT INTO dbo.TipoComprobante (Codigo, Descripcion)
	VALUES ('FACTURA', N'Comprobante de venta académico (sin validez tributaria)');
IF NOT EXISTS (SELECT 1 FROM dbo.TipoComprobante WHERE Codigo = 'NOTA_CREDITO')
	INSERT INTO dbo.TipoComprobante (Codigo, Descripcion)
	VALUES ('NOTA_CREDITO', N'Nota de crédito académica (reembolso simulado; sin validez tributaria)');
IF NOT EXISTS (SELECT 1 FROM dbo.EstadoComprobante WHERE Codigo = 'EMITIDO')
	INSERT INTO dbo.EstadoComprobante (Codigo, Descripcion) VALUES ('EMITIDO', N'Comprobante emitido');

-- CAMBIO DP-05: sustituye unicamente el emisor de ejemplo pendiente; conserva datos configurados por el administrador.
UPDATE dbo.DatosEmisor
   SET NombreComercial = N'Aerolinea VIP - Proyecto academico',
	   Identificacion = N'NO APLICA - EMISOR ACADEMICO',
	   Direccion = N'Proyecto academico',
	   LeyendaComprobante = N'Documento de demostracion academica. No constituye factura ni documento tributario autorizado.'
 WHERE DatosEmisorId = 1
   AND (Identificacion LIKE N'PENDIENTE DE DEFINICI%' OR Direccion LIKE N'PENDIENTE DE DEFINICI%');

IF NOT EXISTS (SELECT 1 FROM dbo.EstadoCompra WHERE Codigo = 'ANULADA')
	INSERT INTO dbo.EstadoCompra (Codigo, Descripcion) VALUES ('ANULADA', N'Compra anulada');
IF NOT EXISTS (SELECT 1 FROM dbo.EstadoReserva WHERE Codigo = 'CANCELADA')
	INSERT INTO dbo.EstadoReserva (Codigo, Descripcion) VALUES ('CANCELADA', N'Reserva cancelada');
IF NOT EXISTS (SELECT 1 FROM dbo.EstadoAsignacion WHERE Codigo = 'CANCELADA')
	INSERT INTO dbo.EstadoAsignacion (Codigo, Descripcion, OcupaInventario, EstadoVisual)
	VALUES ('CANCELADA', N'Asiento cancelado (vuelve a estar disponible)', 0, 'DISPONIBLE');
GO

IF OBJECT_ID(N'dbo.NotaCreditoAcademica', N'U') IS NULL
BEGIN
	CREATE TABLE dbo.NotaCreditoAcademica (
		NotaCreditoId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_NotaCreditoAcademica PRIMARY KEY,
		CompraId INT NOT NULL,
		PagoId INT NOT NULL,
		ComprobanteId INT NOT NULL,
		ImporteReembolsado DECIMAL(12,2) NOT NULL,
		PorcentajeReembolso TINYINT NOT NULL,
		FechaRegistro DATETIME2(0) NOT NULL CONSTRAINT DF_NotaCreditoAcademica_Fecha DEFAULT (SYSDATETIME()),
		CONSTRAINT FK_NotaCredito_Compra FOREIGN KEY (CompraId) REFERENCES dbo.Compra (CompraId),
		CONSTRAINT FK_NotaCredito_Pago FOREIGN KEY (PagoId) REFERENCES dbo.Pago (PagoId),
		CONSTRAINT FK_NotaCredito_Comprobante FOREIGN KEY (ComprobanteId) REFERENCES dbo.Comprobante (ComprobanteId),
		CONSTRAINT UQ_NotaCredito_Compra UNIQUE (CompraId),
		CONSTRAINT UQ_NotaCredito_Pago UNIQUE (PagoId),
		CONSTRAINT UQ_NotaCredito_Comprobante UNIQUE (ComprobanteId),
		CONSTRAINT CK_NotaCredito_Importe CHECK (ImporteReembolsado >= 0),
		CONSTRAINT CK_NotaCredito_Porcentaje CHECK (PorcentajeReembolso BETWEEN 1 AND 100)
	);
END;
GO

IF OBJECT_ID(N'dbo.trg_Vuelo_Validar', N'TR') IS NOT NULL DROP TRIGGER dbo.trg_Vuelo_Validar;
GO
CREATE TRIGGER dbo.trg_Vuelo_Validar
ON dbo.Vuelo
AFTER INSERT, UPDATE
AS
BEGIN
	SET NOCOUNT ON;
	IF NOT EXISTS (SELECT 1 FROM inserted) RETURN;

	IF EXISTS (SELECT 1 FROM inserted i JOIN dbo.Frecuencia f ON f.FrecuenciaId = i.FrecuenciaId
			   WHERE (DATEDIFF(DAY, CAST('19000101' AS DATE), i.FechaOperacion) % 7) + 1 <> f.DiaSemana)
		THROW 50001, N'La fecha del vuelo no corresponde al día de operación autorizado para esa frecuencia.', 1;

	IF EXISTS (SELECT 1 FROM inserted i JOIN dbo.Frecuencia f ON f.FrecuenciaId = i.FrecuenciaId
			   LEFT JOIN deleted d ON d.VueloId = i.VueloId
			   WHERE d.VueloId IS NULL AND f.Activa = 0)
		THROW 50002, N'No se puede programar un vuelo sobre una frecuencia inactiva.', 1;

	-- DP-02: un vuelo sin aeronave asignada se conserva como programado y no se comercializa.
	IF EXISTS (SELECT 1 FROM inserted i WHERE i.AeronaveId IS NULL AND i.EstadoVueloCodigo <> 'PROGRAMADO')
		THROW 50005, N'Un vuelo sin aeronave asignada debe permanecer PROGRAMADO.', 1;

	IF EXISTS (SELECT 1 FROM inserted i JOIN deleted d ON d.VueloId = i.VueloId
			   WHERE d.EstadoVueloCodigo = 'COMPLETADO'
				 AND ISNULL(d.AeronaveId, -1) <> ISNULL(i.AeronaveId, -1))
		THROW 50003, N'No se puede cambiar la aeronave de un vuelo completado.', 1;

	IF EXISTS (SELECT 1 FROM inserted i JOIN dbo.Aeronave a ON a.AeronaveId = i.AeronaveId
			   LEFT JOIN deleted d ON d.VueloId = i.VueloId
			   WHERE a.EstadoAeronaveCodigo <> 'DISPONIBLE'
				 AND (d.VueloId IS NULL OR ISNULL(d.AeronaveId, -1) <> i.AeronaveId))
		THROW 50003, N'La aeronave seleccionada no está disponible para nuevas asignaciones.', 1;

	DECLARE @preparacionMinutos INT = (SELECT Valor FROM dbo.ParametroPolitica WHERE Codigo = 'PreparacionAeronaveMinutos');
	DECLARE @liberacionMinutos INT = (SELECT Valor FROM dbo.ParametroPolitica WHERE Codigo = 'LiberacionAeronaveMinutos');
	IF @preparacionMinutos IS NULL OR @liberacionMinutos IS NULL
		THROW 50004, N'Faltan parametros aprobados de preparacion/liberacion de aeronaves.', 1;

	IF EXISTS (SELECT 1
			   FROM (SELECT vd.VueloId, vd.AeronaveId, vd.AeropuertoOrigenId,
							vd.SalidaProgramada, vd.LlegadaProgramada,
							LAG(vd.VueloId) OVER (PARTITION BY vd.AeronaveId ORDER BY vd.SalidaProgramada, vd.VueloId) AS VueloAnterior,
							LAG(vd.AeropuertoDestinoId) OVER (PARTITION BY vd.AeronaveId ORDER BY vd.SalidaProgramada, vd.VueloId) AS DestinoAnterior,
							LAG(vd.LlegadaProgramada) OVER (PARTITION BY vd.AeronaveId ORDER BY vd.SalidaProgramada, vd.VueloId) AS LlegadaAnterior
					 FROM dbo.vw_VueloDetalle vd
					 WHERE vd.AeronaveId IN (SELECT AeronaveId FROM inserted WHERE AeronaveId IS NOT NULL
											 UNION SELECT AeronaveId FROM deleted WHERE AeronaveId IS NOT NULL)
					   AND vd.EstadoVueloCodigo <> 'CANCELADO') v
			   WHERE (v.VueloId IN (SELECT VueloId FROM inserted UNION SELECT VueloId FROM deleted)
					  OR v.VueloAnterior IN (SELECT VueloId FROM inserted UNION SELECT VueloId FROM deleted))
				 AND v.DestinoAnterior IS NOT NULL
				 AND (v.AeropuertoOrigenId <> v.DestinoAnterior
					  OR DATEADD(MINUTE, -@preparacionMinutos, v.SalidaProgramada) <
						 DATEADD(MINUTE, @liberacionMinutos, v.LlegadaAnterior)))
		THROW 50004, N'La aeronave no tiene tiempo suficiente de preparacion/desplazamiento o continuidad de aeropuerto.', 1;
END;
GO

IF OBJECT_ID(N'dbo.trg_Aeronave_ValidarMapaAsientos', N'TR') IS NOT NULL DROP TRIGGER dbo.trg_Aeronave_ValidarMapaAsientos;
GO
CREATE TRIGGER dbo.trg_Aeronave_ValidarMapaAsientos
ON dbo.Aeronave
AFTER INSERT, UPDATE
AS
BEGIN
	SET NOCOUNT ON;
	IF EXISTS
	(
		SELECT 1
		FROM inserted i
		JOIN dbo.ModeloAeronave m ON m.ModeloAeronaveId = i.ModeloAeronaveId
		WHERE (SELECT COUNT(*) FROM dbo.Asiento s WHERE s.ModeloAeronaveId = m.ModeloAeronaveId) <> m.CapacidadAsientos
		   OR EXISTS (SELECT 1 FROM dbo.Asiento s
					  WHERE s.ModeloAeronaveId = m.ModeloAeronaveId
						AND s.NumeroAsiento > m.CapacidadAsientos)
	)
		THROW 50006, N'No se puede registrar la aeronave: el mapa de asientos del modelo esta incompleto.', 1;
END;
GO

IF OBJECT_ID(N'dbo.trg_Asiento_ProtegerMapaAeronave', N'TR') IS NOT NULL DROP TRIGGER dbo.trg_Asiento_ProtegerMapaAeronave;
GO
CREATE TRIGGER dbo.trg_Asiento_ProtegerMapaAeronave
ON dbo.Asiento
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
	SET NOCOUNT ON;
	IF EXISTS
	(
		SELECT 1
		FROM
		(
			SELECT ModeloAeronaveId FROM inserted
			UNION
			SELECT ModeloAeronaveId FROM deleted
		) modelos
		JOIN dbo.Aeronave a ON a.ModeloAeronaveId = modelos.ModeloAeronaveId
	)
		THROW 50007, N'No se puede modificar el mapa de asientos de un modelo asignado a aeronaves.', 1;
END;
GO

IF OBJECT_ID(N'dbo.trg_ModeloAeronave_ValidarCapacidad', N'TR') IS NOT NULL DROP TRIGGER dbo.trg_ModeloAeronave_ValidarCapacidad;
GO
CREATE TRIGGER dbo.trg_ModeloAeronave_ValidarCapacidad
ON dbo.ModeloAeronave
AFTER UPDATE
AS
BEGIN
	SET NOCOUNT ON;
	IF UPDATE(CapacidadAsientos) AND EXISTS
	(
		SELECT 1
		FROM inserted i
		WHERE EXISTS (SELECT 1 FROM dbo.Asiento s WHERE s.ModeloAeronaveId = i.ModeloAeronaveId)
		  AND ((SELECT COUNT(*) FROM dbo.Asiento s WHERE s.ModeloAeronaveId = i.ModeloAeronaveId) <> i.CapacidadAsientos
			   OR EXISTS (SELECT 1 FROM dbo.Asiento s
						  WHERE s.ModeloAeronaveId = i.ModeloAeronaveId AND s.NumeroAsiento > i.CapacidadAsientos))
	)
		THROW 50008, N'La capacidad del modelo debe coincidir con la cantidad de asientos configurados.', 1;
END;
GO

IF OBJECT_ID(N'dbo.trg_TramoItinerario_Validar', N'TR') IS NOT NULL DROP TRIGGER dbo.trg_TramoItinerario_Validar;
GO
CREATE TRIGGER dbo.trg_TramoItinerario_Validar
ON dbo.TramoItinerario
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
	SET NOCOUNT ON;
	DECLARE @conexionMinutos INT = (SELECT Valor FROM dbo.ParametroPolitica WHERE Codigo = 'ConexionMinimaMinutos');
	IF @conexionMinutos IS NULL
		THROW 50021, N'Falta el parametro aprobado de conexion minima.', 1;

	IF EXISTS (SELECT 1
			   FROM (SELECT vd.AeropuertoDestinoId, vd.LlegadaProgramada,
							LEAD(vd.AeropuertoOrigenId) OVER (PARTITION BY t.ItinerarioId ORDER BY t.Orden) AS SiguienteOrigen,
							LEAD(vd.SalidaProgramada) OVER (PARTITION BY t.ItinerarioId ORDER BY t.Orden) AS SiguienteSalida
					 FROM dbo.TramoItinerario t
					 JOIN dbo.vw_VueloDetalle vd ON vd.VueloId = t.VueloId
					 WHERE t.ItinerarioId IN (SELECT ItinerarioId FROM inserted UNION SELECT ItinerarioId FROM deleted)) x
			   WHERE x.SiguienteOrigen IS NOT NULL
				 AND (x.SiguienteOrigen <> x.AeropuertoDestinoId
					  OR CONVERT(DATE, x.SiguienteSalida) <> CONVERT(DATE, x.LlegadaProgramada)
					  OR x.SiguienteSalida < DATEADD(MINUTE, @conexionMinutos, x.LlegadaProgramada)))
		THROW 50021, N'Los tramos requieren continuidad de aeropuerto, misma fecha y al menos el tiempo minimo aprobado de conexion.', 1;
END;
GO

-- DP-09: la salida y el regreso a Galápagos se comercializan como itinerarios separados.
IF OBJECT_ID('tempdb..#ItinerariosGalapagosPorSeparar') IS NOT NULL DROP TABLE #ItinerariosGalapagosPorSeparar;
SELECT i.ItinerarioId, i.Referencia, i.Nombre, i.EstadoItinerarioCodigo,
	   SUBSTRING(i.Referencia, 4, 8) AS FechaReferencia
INTO #ItinerariosGalapagosPorSeparar
FROM dbo.Itinerario i
WHERE i.Referencia LIKE 'IT-[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'
  AND (SELECT COUNT(*) FROM dbo.TramoItinerario t WHERE t.ItinerarioId = i.ItinerarioId) = 4;

INSERT INTO dbo.Itinerario (Referencia, Nombre, EstadoItinerarioCodigo)
SELECT 'IT-RET-' + FechaReferencia,
	   N'Galápagos – Guayaquil – Quito · ' + STUFF(STUFF(FechaReferencia, 5, 0, '-'), 8, 0, '-'),
	   EstadoItinerarioCodigo
FROM #ItinerariosGalapagosPorSeparar;

UPDATE original
   SET Referencia = 'IT-OUT-' + separar.FechaReferencia,
	   Nombre = N'Quito – Guayaquil – Galápagos · ' + STUFF(STUFF(separar.FechaReferencia, 5, 0, '-'), 8, 0, '-')
FROM dbo.Itinerario original
JOIN #ItinerariosGalapagosPorSeparar separar ON separar.ItinerarioId = original.ItinerarioId;

;WITH TramosOrdenados AS
(
	SELECT t.TramoItinerarioId, t.ItinerarioId, t.Orden,
		   ROW_NUMBER() OVER (PARTITION BY t.ItinerarioId ORDER BY t.Orden) AS Posicion,
		   SUBSTRING(i.Referencia, 8, 8) AS FechaReferencia
	FROM dbo.TramoItinerario t
	JOIN dbo.Itinerario i ON i.ItinerarioId = t.ItinerarioId
	WHERE i.Referencia LIKE 'IT-OUT-[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'
)
UPDATE tramo
   SET ItinerarioId = regreso.ItinerarioId,
	   Orden = ordenado.Posicion - 2
FROM dbo.TramoItinerario tramo
JOIN TramosOrdenados ordenado ON ordenado.TramoItinerarioId = tramo.TramoItinerarioId
JOIN dbo.Itinerario regreso ON regreso.Referencia = 'IT-RET-' + ordenado.FechaReferencia
WHERE ordenado.Posicion > 2;

DROP TABLE #ItinerariosGalapagosPorSeparar;
GO

-- CAMBIO DP-10: conserva incidencias de vuelos cancelados para revisión administrativa.
IF OBJECT_ID(N'dbo.RevisionOperativa', N'U') IS NULL
BEGIN
	CREATE TABLE dbo.RevisionOperativa (
		RevisionOperativaId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_RevisionOperativa PRIMARY KEY,
		VueloId INT NOT NULL,
		ItinerarioId INT NULL,
		ReservaId INT NULL,
		Motivo NVARCHAR(300) NOT NULL,
		FechaRegistro DATETIME2(0) NOT NULL CONSTRAINT DF_RevisionOperativa_Fecha DEFAULT (SYSDATETIME()),
		Estado VARCHAR(20) NOT NULL CONSTRAINT DF_RevisionOperativa_Estado DEFAULT ('PENDIENTE'),
		FechaResolucion DATETIME2(0) NULL,
		CONSTRAINT FK_RevisionOperativa_Vuelo FOREIGN KEY (VueloId) REFERENCES dbo.Vuelo (VueloId),
		CONSTRAINT FK_RevisionOperativa_Itinerario FOREIGN KEY (ItinerarioId) REFERENCES dbo.Itinerario (ItinerarioId),
		CONSTRAINT FK_RevisionOperativa_Reserva FOREIGN KEY (ReservaId) REFERENCES dbo.Reserva (ReservaId),
		CONSTRAINT CK_RevisionOperativa_Estado CHECK (Estado IN ('PENDIENTE', 'RESUELTA'))
	);
	CREATE INDEX IX_RevisionOperativa_Pendiente
		ON dbo.RevisionOperativa (Estado, FechaRegistro, VueloId);
END;
GO

IF OBJECT_ID(N'dbo.trg_Vuelo_RegistrarCancelacion', N'TR') IS NOT NULL
	DROP TRIGGER dbo.trg_Vuelo_RegistrarCancelacion;
GO
CREATE TRIGGER dbo.trg_Vuelo_RegistrarCancelacion
ON dbo.Vuelo
AFTER UPDATE
AS
BEGIN
	SET NOCOUNT ON;

	;WITH VuelosCancelados AS
	(
		SELECT i.VueloId
		FROM inserted i
		JOIN deleted d ON d.VueloId = i.VueloId
		WHERE i.EstadoVueloCodigo = 'CANCELADO'
		  AND d.EstadoVueloCodigo <> 'CANCELADO'
	),
	ItinerariosAfectados AS
	(
		SELECT DISTINCT t.ItinerarioId
		FROM dbo.TramoItinerario t
		JOIN VuelosCancelados c ON c.VueloId = t.VueloId
	)
	UPDATE i
	   SET EstadoItinerarioCodigo = 'INACTIVO'
	FROM dbo.Itinerario i
	JOIN ItinerariosAfectados a ON a.ItinerarioId = i.ItinerarioId;

	;WITH VuelosCancelados AS
	(
		SELECT i.VueloId
		FROM inserted i
		JOIN deleted d ON d.VueloId = i.VueloId
		WHERE i.EstadoVueloCodigo = 'CANCELADO'
		  AND d.EstadoVueloCodigo <> 'CANCELADO'
	),
	Incidencias AS
	(
		SELECT DISTINCT c.VueloId, t.ItinerarioId, r.ReservaId
		FROM VuelosCancelados c
		LEFT JOIN dbo.TramoItinerario t ON t.VueloId = c.VueloId
		LEFT JOIN dbo.ReservaPasajeroVuelo a
			ON a.VueloId = c.VueloId AND a.EstadoAsignacionCodigo IN ('PENDIENTE', 'CONFIRMADA')
		LEFT JOIN dbo.Reserva r
			ON r.ReservaId = a.ReservaId
		   AND (r.EstadoReservaCodigo IN ('CONFIRMADA', 'SOLICITUD_CANCELACION')
				OR (r.EstadoReservaCodigo = 'PENDIENTE' AND r.FechaExpiracion > SYSDATETIME()))
	)
	INSERT INTO dbo.RevisionOperativa (VueloId, ItinerarioId, ReservaId, Motivo)
	SELECT inc.VueloId, inc.ItinerarioId, inc.ReservaId,
		   N'Vuelo cancelado; validar itinerario y comunicar/gestionar las reservas afectadas según DP-10.'
	FROM Incidencias inc
	WHERE NOT EXISTS
	(
		SELECT 1
		FROM dbo.RevisionOperativa existente
		WHERE existente.Estado = 'PENDIENTE'
		  AND existente.VueloId = inc.VueloId
		  AND ISNULL(existente.ItinerarioId, 0) = ISNULL(inc.ItinerarioId, 0)
		  AND ISNULL(existente.ReservaId, 0) = ISNULL(inc.ReservaId, 0)
	);
END;
GO
