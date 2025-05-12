namespace SistemaAgendamento.DTOs
{
    public record DefinirDispDto(string Medico, int EspecialidadeId, string DiaSemana, string HoraInicio, string HoraFim, int DuracaoConsultaMinutos);
}