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

// Configure PostgreSQL
builder.Services.AddDbContext<AppDbContext>(opts =>
    opts.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure CORS
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo {
        Title = "API de Agendamento Médico",
        Version = "v1"
    });
});

var app = builder.Build();
app.UseCors();
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Agendamento Médico V1");
    c.RoutePrefix = string.Empty;
});

// Endpoints implementation (as provided)
app.MapPost("/api/especialidades", async (NomeDto dto, AppDbContext db) =>
{
    var esp = new Especialidade { Nome = dto.Nome };
    db.Especialidades.Add(esp);
    await db.SaveChangesAsync();
    return Results.Created($"/api/especialidades/{esp.Id}", esp);
}).WithTags("Especialidades");

app.MapGet("/api/especialidades", async (AppDbContext db) =>
    await db.Especialidades.ToListAsync()
).WithTags("Especialidades");

app.MapPost("/api/convenios", async (NomeDto dto, AppDbContext db) =>
{
    var conv = new Convenio { Nome = dto.Nome };
    db.Convenios.Add(conv);
    await db.SaveChangesAsync();
    return Results.Created($"/api/convenios/{conv.Id}", conv);
}).WithTags("Convênios");

app.MapGet("/api/convenios", async (AppDbContext db) =>
    await db.Convenios.ToListAsync()
).WithTags("Convênios");

app.MapPost("/api/disponibilidades/definir", async (DefinirDispDto dto, AppDbContext db) =>
{
    var di = new Disponibilidade {
        Medico = dto.Medico,
        EspecialidadeId = dto.EspecialidadeId,
        DiaSemana = dto.DiaSemana,
        HoraInicio = dto.HoraInicio,
        HoraFim = dto.HoraFim,
        DuracaoConsultaMinutos = dto.DuracaoConsultaMinutos
    };
    db.Disponibilidades.Add(di);
    await db.SaveChangesAsync();
    return Results.Created($"/api/disponibilidades/{di.Id}", di);
}).WithTags("Disponibilidades");

app.MapPost("/api/disponibilidades", async (QueryDispDto q, AppDbContext db) =>
{
    var dia = q.Data.DayOfWeek.ToString();
    var blocos = await db.Disponibilidades
        .Where(d => d.EspecialidadeId == q.EspecialidadeId
                 && d.DiaSemana.Equals(dia, StringComparison.OrdinalIgnoreCase)
                 && (string.IsNullOrEmpty(q.Medico) || d.Medico == q.Medico))
        .ToListAsync();

    var ags = await db.Agendamentos.ToListAsync();
    var slots = new List<SlotDto>();

    foreach (var b in blocos)
    {
        var start = TimeSpan.Parse(b.HoraInicio);
        var end   = TimeSpan.Parse(b.HoraFim);
        var cur   = start;
        while (cur + TimeSpan.FromMinutes(b.DuracaoConsultaMinutos) <= end)
        {
            var dt = new DateTime(q.Data.Year, q.Data.Month, q.Data.Day, cur.Hours, cur.Minutes, 0);
            var existing = ags.FirstOrDefault(a => a.DataHora == dt);
            if (existing != null)
                slots.Add(new SlotDto(cur.ToString("HH:mm"), (cur + TimeSpan.FromMinutes(b.DuracaoConsultaMinutos)).ToString("HH:mm"), false, existing.Id, existing.Paciente));
            else
                slots.Add(new SlotDto(cur.ToString("HH:mm"), (cur + TimeSpan.FromMinutes(b.DuracaoConsultaMinutos)).ToString("HH:mm"), true));
            cur = cur.Add(TimeSpan.FromMinutes(b.DuracaoConsultaMinutos));
        }
    }

    return Results.Ok(slots);
}).WithTags("Disponibilidades");

app.MapPost("/api/agendamentos", async (AgendarDto dto, AppDbContext db) =>
{
    if (await db.Agendamentos.AnyAsync(a => a.DataHora == dto.DataHora))
        return Results.Conflict("Horário já ocupado.");

    var block = await db.Disponibilidades.FirstOrDefaultAsync(d =>
        d.EspecialidadeId == dto.EspecialidadeId
        && TimeSpan.Parse(d.HoraInicio) <= dto.DataHora.TimeOfDay
        && TimeSpan.Parse(d.HoraFim) >= dto.DataHora.TimeOfDay + TimeSpan.FromMinutes(d.DuracaoConsultaMinutos)
        && d.DiaSemana.Equals(dto.DataHora.DayOfWeek.ToString(), StringComparison.OrdinalIgnoreCase));

    if (block == null)
        return Results.BadRequest("Horário não disponível.");

    var ag = new Agendamento {
        Paciente = dto.Paciente,
        EspecialidadeId = dto.EspecialidadeId,
        ConvenioId = dto.ConvenioId,
        DataHora = dto.DataHora,
        Medico = block.Medico
    };
    db.Agendamentos.Add(ag);
    await db.SaveChangesAsync();
    return Results.Created($"/api/agendamentos/{ag.Id}", ag);
}).WithTags("Agendamentos");

app.MapGet("/api/agendamentos", async (DateTime? di, DateTime? df, string paciente, AppDbContext db) =>
{
    var q = db.Agendamentos.AsQueryable();
    if (di.HasValue) q = q.Where(a => a.DataHora >= di.Value);
    if (df.HasValue) q = q.Where(a => a.DataHora <= df.Value);
    if (!string.IsNullOrEmpty(paciente)) q = q.Where(a => a.Paciente == paciente);
    return Results.Ok(await q.ToListAsync());
}).WithTags("Agendamentos");

app.MapPost("/api/atendimentos", async (AtendimentoDto dto, AppDbContext db) =>
{
    var at = new Atendimento {
        AgendamentoId = dto.AgendamentoId,
        DataAtendimento = DateTime.UtcNow,
        Observacoes = dto.Observacoes
    };
    db.Atendimentos.Add(at);
    await db.SaveChangesAsync();
    return Results.Created($"/api/atendimentos/{at.Id}", at);
}).WithTags("Atendimentos");

app.MapGet("/api/atendimentos", async (DateTime? di, DateTime? df, string paciente, AppDbContext db) =>
{
    var atds = await db.Atendimentos.ToListAsync();
    var ags  = await db.Agendamentos.ToListAsync();
    var result = (from at in atds
                  join ag in ags on at.AgendamentoId equals ag.Id
                  where (!di.HasValue || at.DataAtendimento >= di.Value)
                     && (!df.HasValue || at.DataAtendimento <= df.Value)
                     && (string.IsNullOrEmpty(paciente) || ag.Paciente == paciente)
                  select at).ToList();
    return Results.Ok(result);
}).WithTags("Atendimentos");

app.Run();