using backend.DTOs;
using backend.Models;

namespace backend.Services.Helpers;

/// <summary>
/// Une lecturas SIGAFI + espejo local sin duplicar (prioridad SIGAFI en choque de clave).
/// </summary>
public static class SigafiLocalReadMerge
{
    public static List<ClaseActiva> MergeClasesActivas(
        IReadOnlyList<ClaseActiva> desdeSigafi,
        IReadOnlyList<ClaseActiva> desdeLocal)
    {
        var map = new Dictionary<int, ClaseActiva>();
        foreach (var r in desdeSigafi)
            map[r.idPractica] = r;
        foreach (var r in desdeLocal)
        {
            if (!map.ContainsKey(r.idPractica))
                map[r.idPractica] = r;
        }

        return map.Values
            .OrderByDescending(x => x.salida)
            .ToList();
    }

    public static List<AlertaMantenimiento> MergeAlertasVehiculo(
        IReadOnlyList<AlertaMantenimiento> desdeSigafi,
        IReadOnlyList<AlertaMantenimiento> desdeLocal)
    {
        var map = new Dictionary<int, AlertaMantenimiento>();
        foreach (var r in desdeSigafi)
            map[r.id_vehiculo] = r;
        foreach (var r in desdeLocal)
        {
            if (!map.ContainsKey(r.id_vehiculo))
                map[r.id_vehiculo] = r;
        }

        return map.Values
            .OrderBy(x => x.numero_vehiculo)
            .ToList();
    }

    public static List<ReportePracticasDTO> MergeReportePracticas(
        IReadOnlyList<ReportePracticasDTO> desdeSigafi,
        IReadOnlyList<ReportePracticasDTO> desdeLocal)
    {
        var map = new Dictionary<int, ReportePracticasDTO>();
        foreach (var r in desdeSigafi)
            map[r.idPractica] = r;
        foreach (var r in desdeLocal)
        {
            if (!map.ContainsKey(r.idPractica))
                map[r.idPractica] = r;
        }

        return map.Values
            .OrderByDescending(x => x.idPractica)
            .ToList();
    }
}
