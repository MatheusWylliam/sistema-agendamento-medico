using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using SistemaAgendamento.Data;
using SistemaAgendamento.Models;
using SistemaAgendamento.DTOs;
using System;
using System.Linq;
using System.Collections.Generic;

var builder = WebApplication.CreateBuilder(args);

// essa parte configura oo DbContext para PostgreSQL pelo  Entity Framework Core
builder.Services.AddDbContext<AppDbContext>(opts =>
    opts.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

// essa parte habilita CORS pra permitir requisições de qualquer origem, método e cabeçalho
builder.Services.AddCors(o =>
    o.AddDefaultPolicy(p =>
        p.AllowAnyOrigin()
         .AllowAnyMethod()
         .AllowAnyHeader()
    )
);

// essa parte configura o swagger/openAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo {
        Title   = "API de Agendamento Médico",
        Version = "v1"
    });
});

var app = builder.Build();

// essa parte ativa o CORS, swagger e o swaggerui
app.UseCors();
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    // essa parte define a URL do JSON do swagger e remove prefixo
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Agendamento Médico V1");
    c.RoutePrefix = string.Empty;
});

//                   os endpoints (usando minimal APIs)

// Especialidades
// POST /api/especialidades — pra cadastrar nova especialidade
app.MapPost("/api/especialidades", async (NomeDto dto, AppDbContext db) =>
{
    var esp = new Especialidade { Nome = dto.Nome };
    db.Especialidades.Add(esp);
    await db.SaveChangesAsync();
    return Results.Created($"/api/especialidades/{esp.Id}", esp);
}).WithTags("Especialidades");

// GET /api/especialidades — prs listar todas as especialidades
app.MapGet("/api/especialidades", async (AppDbContext db) =>
    await db.Especialidades.ToListAsync()
).WithTags("Especialidades");

// Convênios
// POST /api/convenios — pra cadastrar novo convênio
app.MapPost("/api/convenios", async (NomeDto dto, AppDbContext db) =>
{
    var conv = new Convenio { Nome = dto.Nome };
    db.Convenios.Add(conv);
    await db.SaveChangesAsync();
    return Results.Created($"/api/convenios/{conv.Id}", conv);
}).WithTags("Convênios");

// GET /api/convenios — pra listar todos os convênios
app.MapGet("/api/convenios", async (AppDbContext db) =>
    await db.Convenios.ToListAsync()
).WithTags("Convênios");

// Disponibilidades
// POST /api/disponibilidades/definir — pra cadastrar bloco de horários
app.MapPost("/api/disponibilidades/definir", async (DefinirDispDto dto, AppDbContext db) =>
{
    var di = new Disponibilidade {
        Medico                  = dto.Medico,
        EspecialidadeId         = dto.EspecialidadeId,
        DiaSemana               = dto.DiaSemana,
        HoraInicio              = dto.HoraInicio,
        HoraFim                 = dto.HoraFim,
        DuracaoConsultaMinutos  = dto.DuracaoConsultaMinutos
    };
    db.Disponibilidades.Add(di);
    await db.SaveChangesAsync();
    return Results.Created($"/api/disponibilidades/{di.Id}", di);
}).WithTags("Disponibilidades");

// POST /api/disponibilidades — pra gerar slots (livres/ocupados) pra uma data
app.MapPost("/api/disponibilidades", async (QueryDispDto q, AppDbContext db) =>
{
    // pra converter o dia da semana pra comparar com Disponibilidade.DiaSemana
    var dia = q.Data.DayOfWeek.ToString();

    // pra buscar blocos que batem com especialidade, dia e médico
    var blocos = await db.Disponibilidades
        .Where(d =>
            d.EspecialidadeId == q.EspecialidadeId
         && d.DiaSemana.Equals(dia, StringComparison.OrdinalIgnoreCase)
         && (string.IsNullOrEmpty(q.Medico) || d.Medico == q.Medico)
        )
        .ToListAsync();

    // pra carregar todos os agendamentos pra verificar conflitos
    var ags = await db.Agendamentos.ToListAsync();
    var slots = new List<SlotDto>();

    // pra cada bloco, gera intervalos de acordo com a duração e marca disponíveis/ocupados
    foreach (var b in blocos)
    {
        var start = TimeSpan.Parse(b.HoraInicio);
        var end   = TimeSpan.Parse(b.HoraFim);
        var cur   = start;

        while (cur + TimeSpan.FromMinutes(b.DuracaoConsultaMinutos) <= end)
        {
            // pra montar a data completa do slot
            var dt = new DateTime(
                q.Data.Year, q.Data.Month, q.Data.Day,
                cur.Hours, cur.Minutes, 0
            );

            // pra verificar se já existe agendamento nesse horário
            var existing = ags.FirstOrDefault(a => a.DataHora == dt);

            if (existing != null)
            {
                // pro slot ocupado: retorna false e dados do paciente
                slots.Add(new SlotDto(
                    cur.ToString("HH:mm"),
                    (cur + TimeSpan.FromMinutes(b.DuracaoConsultaMinutos)).ToString("HH:mm"),
                    false,
                    existing.Id,
                    existing.Paciente
                ));
            }
            else
            {
                // slot livre
                slots.Add(new SlotDto(
                    cur.ToString("HH:mm"),
                    (cur + TimeSpan.FromMinutes(b.DuracaoConsultaMinutos)).ToString("HH:mm"),
                    true
                ));
            }

            // pra avançar pro próximo intervalo
            cur = cur.Add(TimeSpan.FromMinutes(b.DuracaoConsultaMinutos));
        }
    }

    return Results.Ok(slots);
}).WithTags("Disponibilidades");

// Agendamentos
// POST /api/agendamentos — pra criar um novo agendamento
app.MapPost("/api/agendamentos", async (AgendarDto dto, AppDbContext db) =>
{
    // pra evitar duplicidade de DataHora
    if (await db.Agendamentos.AnyAsync(a => a.DataHora == dto.DataHora))
        return Results.Conflict("Horário já ocupado.");

    // pra verificar se a DataHora solicitada tá dentro de algum bloco disponível
    var block = await db.Disponibilidades.FirstOrDefaultAsync(d =>
        d.EspecialidadeId == dto.EspecialidadeId
     && TimeSpan.Parse(d.HoraInicio) <= dto.DataHora.TimeOfDay
     && TimeSpan.Parse(d.HoraFim) >= dto.DataHora.TimeOfDay + TimeSpan.FromMinutes(d.DuracaoConsultaMinutos)
     && d.DiaSemana.Equals(dto.DataHora.DayOfWeek.ToString(), StringComparison.OrdinalIgnoreCase)
    );

    if (block == null)
        return Results.BadRequest("Horário não disponível.");

    // pra criar o agendamento
    var ag = new Agendamento {
        Paciente         = dto.Paciente,
        EspecialidadeId  = dto.EspecialidadeId,
        ConvenioId       = dto.ConvenioId,
        DataHora         = dto.DataHora,
        Medico           = block.Medico
    };
    db.Agendamentos.Add(ag);
    await db.SaveChangesAsync();
    return Results.Created($"/api/agendamentos/{ag.Id}", ag);
}).WithTags("Agendamentos");

// GET /api/agendamentos — pra lista agendamentos com filtros opcionais
app.MapGet("/api/agendamentos", async (DateTime? di, DateTime? df, string paciente, AppDbContext db) =>
{
    var q = db.Agendamentos.AsQueryable();
    if (di.HasValue)      q = q.Where(a => a.DataHora >= di.Value);
    if (df.HasValue)      q = q.Where(a => a.DataHora <= df.Value);
    if (!string.IsNullOrEmpty(paciente))
                          q = q.Where(a => a.Paciente == paciente);

    return Results.Ok(await q.ToListAsync());
}).WithTags("Agendamentos");

// Atendimentos
// POST /api/atendimentos — pra marcar um agendamento como atendido
app.MapPost("/api/atendimentos", async (AtendimentoDto dto, AppDbContext db) =>
{
    var at = new Atendimento {
        AgendamentoId   = dto.AgendamentoId,
        DataAtendimento = DateTime.UtcNow,
        Observacoes     = dto.Observacoes
    };
    db.Atendimentos.Add(at);
    await db.SaveChangesAsync();
    return Results.Created($"/api/atendimentos/{at.Id}", at);
}).WithTags("Atendimentos");

// GET /api/atendimentos — pra listar atendimentos com filtros opcionais
app.MapGet("/api/atendimentos", async (DateTime? di, DateTime? df, string paciente, AppDbContext db) =>
{
    // pra carregar todos os atendimentos e agendamentos pra relacionar dados
    var atds = await db.Atendimentos.ToListAsync();
    var ags  = await db.Agendamentos.ToListAsync();

    // pra realizar um join em memória pra filtrar por data e paciente
    var result = (from at in atds
                  join ag in ags on at.AgendamentoId equals ag.Id
                  where (!di.HasValue || at.DataAtendimento >= di.Value)
                     && (!df.HasValue || at.DataAtendimento <= df.Value)
                     && (string.IsNullOrEmpty(paciente) || ag.Paciente == paciente)
                  select at
                 ).ToList();

    return Results.Ok(result);
}).WithTags("Atendimentos");

// pra niciar a aplicação
app.Run();
