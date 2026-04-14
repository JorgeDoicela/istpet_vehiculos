using System.Threading.Tasks;

namespace backend.Services.Interfaces
{
    public interface IAgendaPanelService
    {
        Task<AgendaLogisticaResponseDto> GetAgendaAsync(int limit = 100);
        Task<AgendaLogisticaResponseDto> GetTodayHistoryAsync(int limit = 50);
    }
}
