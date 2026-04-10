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

        // 1. Cargar SIGAFI como base académica y logística oficial
        foreach (var s in desdeSigafi)
        {
            map[s.idPractica] = s;
        }

        // 2. Reconciliar con Base Local (Verdad operativa/live)
        foreach (var l in desdeLocal)
        {
            if (map.TryGetValue(l.idPractica, out var existing))
            {
                // Reconciliación: Mantener nombres de SIGAFI, pero inyectar TIEMPOS y ESTADOS de Local
                // si local tiene información más reciente o completa.
                
                // Si local tiene hora de llegada y SIGAFI no, usamos local
                if (!string.IsNullOrWhiteSpace(l.horaLlegada) && string.IsNullOrWhiteSpace(existing.horaLlegada))
                {
                    existing.horaLlegada = l.horaLlegada;
                    existing.tiempo = l.tiempo;
                }
                
                // Si local está cancelado, marcamos como cancelado
                if (l.cancelado > 0)
                {
                    existing.cancelado = l.cancelado;
                }

                // Si local tiene observaciones, sumarlas o preferirlas
                if (!string.IsNullOrWhiteSpace(l.observaciones))
                {
                    existing.observaciones = l.observaciones;
                }
            }
            else
            {
                // Es una práctica que solo existe localmente (ej: creada hoy)
                map[l.idPractica] = l;
            }
        }

        return map.Values
            .OrderByDescending(x => x.idPractica)
            .ToList();
    }
}
