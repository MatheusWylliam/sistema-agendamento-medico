using Microsoft.EntityFrameworkCore;
using SistemaAgendamento.Models;

namespace SistemaAgendamento.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> opts) : base(opts) { }
        public DbSet<Especialidade> Especialidades { get; set; } = null!;
        public DbSet<Convenio> Convenios { get; set; } = null!;
        public DbSet<Disponibilidade> Disponibilidades { get; set; } = null!;
        public DbSet<Agendamento> Agendamentos { get; set; } = null!;
        public DbSet<Atendimento> Atendimentos { get; set; } = null!;
    }
}