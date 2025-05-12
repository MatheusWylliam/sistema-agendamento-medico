using System;

namespace SistemaAgendamento.DTOs
{
    public record AgendarDto(string Paciente, int EspecialidadeId, int ConvenioId, DateTime DataHora);
}