using backend.Data;
using backend.Models;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Services.Helpers;

/// <summary>
/// Alinea filas de vehículos en el espejo local con el catálogo actual de SIGAFI.
/// </summary>
public static class SigafiVehicleUpsert
{
    public static async Task MergeFromCentralAsync(
        AppDbContext context,
        IEnumerable<CentralVehiculoDto> centralVehicles,
        CancellationToken cancellationToken = default)
    {
        var normalized = centralVehicles
            .GroupBy(r => !string.IsNullOrWhiteSpace(r.numero_vehiculo)
                ? $"NUM:{r.numero_vehiculo}"
                : !string.IsNullOrWhiteSpace(r.placa)
                    ? $"PLA:{r.placa}"
                    : $"ID:{r.idVehiculo}")
            .Select(g => g.First());

        foreach (var cv in normalized)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var existing = await context.Vehiculos.FirstOrDefaultAsync(v =>
                    v.idVehiculo == cv.idVehiculo
                    || (!string.IsNullOrEmpty(cv.numero_vehiculo) && v.numero_vehiculo == cv.numero_vehiculo)
                    || (!string.IsNullOrEmpty(cv.placa) && v.placa == cv.placa),
                cancellationToken);

            if (existing == null)
            {
                context.Vehiculos.Add(new Vehiculo
                {
                    idVehiculo = cv.idVehiculo,
                    idSubcategoria = cv.idSubcategoria,
                    numero_vehiculo = cv.numero_vehiculo,
                    placa = cv.placa,
                    marca = cv.marca,
                    anio = cv.anio,
                    idCategoria = cv.idCategoria,
                    activo = cv.activo == 1,
                    observacion = cv.observacion,
                    chasis = cv.chasis,
                    motor = cv.motor,
                    modelo = cv.modelo
                });
            }
            else
            {
                existing.idSubcategoria = cv.idSubcategoria;
                existing.numero_vehiculo = cv.numero_vehiculo;
                existing.placa = cv.placa;
                existing.marca = cv.marca;
                existing.anio = cv.anio;
                existing.idCategoria = cv.idCategoria;
                existing.activo = cv.activo == 1;
                existing.observacion = cv.observacion;
                existing.chasis = cv.chasis;
                existing.motor = cv.motor;
                existing.modelo = cv.modelo;
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
