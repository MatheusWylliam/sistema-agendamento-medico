namespace SistemaAgendamento.DTOs
{
    public record SlotDto(string HoraInicio, string HoraFim, bool Disponivel, int? AgendamentoId = null, string? Paciente = null);
}