/*
 * CAMBIO: agrega el estado requerido para registrar solicitudes DP-08 sin cancelar
 * la compra ni liberar asientos mientras la política de reembolsos siga pendiente.
 * Ejecute este script una vez en la base AerolineaVIP existente.
 */
USE AerolineaVIP;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.EstadoReserva WHERE Codigo = 'SOLICITUD_CANCELACION')
BEGIN
	INSERT INTO dbo.EstadoReserva (Codigo, Descripcion)
	VALUES ('SOLICITUD_CANCELACION', N'Solicitud de cancelación pendiente de aprobación; inventario retenido');
END;
GO
