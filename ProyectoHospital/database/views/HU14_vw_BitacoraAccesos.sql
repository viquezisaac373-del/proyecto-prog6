USE Hospital_DB4;
GO

CREATE OR ALTER VIEW dbo.vw_BitacoraAccesos
AS
SELECT
    b.AccesoId,
    ISNULL(CAST(b.UsuarioId AS NVARCHAR(20)), N'Sin registro') AS UsuarioId,
    ISNULL(b.NombreUsuario, N'Sin nombre') AS NombreUsuario,
    CASE WHEN b.UsuarioId IS NULL THEN N'No registrado' ELSE N'Registrado' END AS TipoUsuario,
    ISNULL(b.Ip, N'No disponible') AS Ip,
    b.Accion,
    b.Resultado,
    ISNULL(b.Mensaje, N'Sin descripción') AS Mensaje,
    CONVERT(DATE, b.FechaHora) AS Fecha,
    CONVERT(TIME(0), b.FechaHora) AS Hora
FROM dbo.BitacoraAccesos b;
GO
