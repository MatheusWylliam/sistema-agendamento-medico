using System;

namespace SistemaAgendamento.DTOs
{
    public record QueryDispDto(int EspecialidadeId, DateTime Data, string Medico);
}