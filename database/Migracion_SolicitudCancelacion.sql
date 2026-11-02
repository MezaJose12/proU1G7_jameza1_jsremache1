/*
 * Agrega el estado requerido para registrar solicitudes DP-08 sin cancelar
 * la compra ni liberar asientos mientras la política de reembolsos siga pendiente.
 * Ejecute este script en la base AerolineaVIP existente; es seguro repetirlo.
 */
USE AerolineaVIP;
GO

SET XACT_ABORT ON;
BEGIN TRANSACTION;

DECLARE @estadoReservaCodigoLength smallint;
DECLARE @reservaEstadoCodigoLength smallint;

SELECT @estadoReservaCodigoLength = max_length
FROM sys.columns
WHERE object_id = OBJECT_ID(N'dbo.EstadoReserva') AND name = N'Codigo';

SELECT @reservaEstadoCodigoLength = max_length
FROM sys.columns
WHERE object_id = OBJECT_ID(N'dbo.Reserva') AND name = N'EstadoReservaCodigo';

IF @estadoReservaCodigoLength IS NULL OR @reservaEstadoCodigoLength IS NULL
BEGIN
	THROW 50000, 'No se encontraron las columnas esperadas en AerolineaVIP.', 1;
END;

IF @estadoReservaCodigoLength < 21 OR @reservaEstadoCodigoLength < 21
BEGIN
	IF OBJECT_ID(N'dbo.FK_Reserva_Estado', N'F') IS NOT NULL
		ALTER TABLE dbo.Reserva DROP CONSTRAINT FK_Reserva_Estado;

	IF OBJECT_ID(N'dbo.PK_EstadoReserva', N'PK') IS NOT NULL
		ALTER TABLE dbo.EstadoReserva DROP CONSTRAINT PK_EstadoReserva;

	ALTER TABLE dbo.EstadoReserva ALTER COLUMN Codigo VARCHAR(21) NOT NULL;
	ALTER TABLE dbo.Reserva ALTER COLUMN EstadoReservaCodigo VARCHAR(21) NOT NULL;

	ALTER TABLE dbo.EstadoReserva
		ADD CONSTRAINT PK_EstadoReserva PRIMARY KEY (Codigo);
	ALTER TABLE dbo.Reserva
		ADD CONSTRAINT FK_Reserva_Estado FOREIGN KEY (EstadoReservaCodigo)
		REFERENCES dbo.EstadoReserva (Codigo);
END;

IF NOT EXISTS (SELECT 1 FROM dbo.EstadoReserva WHERE Codigo = 'SOLICITUD_CANCELACION')
BEGIN
	INSERT INTO dbo.EstadoReserva (Codigo, Descripcion)
	VALUES ('SOLICITUD_CANCELACION', N'Solicitud de cancelacion pendiente de aprobacion; inventario retenido');
END;

UPDATE dbo.EstadoReserva
SET Descripcion = N'Solicitud de cancelacion pendiente de aprobacion; inventario retenido'
WHERE Codigo = 'SOLICITUD_CANCELACION';

COMMIT TRANSACTION;
GO
