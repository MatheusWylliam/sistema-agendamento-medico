using System;

namespace SistemaAgendamento.Models
{
    public class Atendimento
    {
        public int Id { get; set; }
        public int AgendamentoId { get; set; }
        public DateTime DataAtendimento { get; set; }
        public string? Observacoes { get; set; }
    }
}