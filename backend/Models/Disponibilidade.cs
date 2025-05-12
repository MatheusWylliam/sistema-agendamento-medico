namespace SistemaAgendamento.Models
{
    public class Disponibilidade
    {
        public int Id { get; set; }
        public string Medico { get; set; } = null!;
        public int EspecialidadeId { get; set; }
        public string DiaSemana { get; set; } = null!;
        public string HoraInicio { get; set; } = null!;
        public string HoraFim { get; set; } = null!;
        public int DuracaoConsultaMinutos { get; set; }
    }
}