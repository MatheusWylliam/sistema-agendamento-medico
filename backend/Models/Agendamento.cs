using System;

namespace SistemaAgendamento.Models
{
    public class Agendamento
    {
        public int Id { get; set; }
        public string Paciente { get; set; } = null!;
        public int EspecialidadeId { get; set; }
        public int ConvenioId { get; set; }
        public DateTime DataHora { get; set; }
        public string Medico { get; set; } = null!;
    }
}