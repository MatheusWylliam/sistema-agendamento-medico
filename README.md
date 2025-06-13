#  Projeto Sistema de Agendamento Médico
Aplicação para gestão de agendamentos médicos, permite cadastro de especialidades, convênios, horários de disponibilidade, agendamentos e atendimentos.
O **back-end** (C#/.NET 8) e o **front-end** (HTML, Tailwind CSS e JavaScript). Docker para uma melhor portabilidade do projeto.

---

## Tecnologias

- **Back-end**: .NET 8 (ASP.NET Core Minimal APIs), Entity Framework Core, Npgsql (PostgreSQL) e Swashbuckle (Swagger);
- **Front-end**: HTML5, Tailwind CSS e JavaScript;
- **Containerização**: Docker e Docker Compose.

---

## Estrutura do projeto 

```
/sistema_agendamento_testewest
├── backend/
│   ├── Dockerfile
│   ├── SistemaAgendamento.csproj
│   ├── appsettings.json
│   ├── Program.cs
│   ├── Data/         # AppDbContext.cs
│   ├── Models/       # Especialidade.cs, Convenio.cs, Disponibilidade.cs, Agendamento.cs, Atendimento.cs
│   └── DTOs/         # NomeDto.cs, DefinirDispDto.cs, QueryDispDto.cs, AgendarDto.cs, AtendimentoDto.cs, SlotDto.cs
│
├── frontend/
│   ├── Dockerfile
│   └── index.html
│
└── docker-compose.yml
```
### Endpoints da API
```
POST /especialidades – Cadastro de especialidades

GET /especialidades – Listagem de especialidades

POST /convenios – Cadastro de convênios

GET /convenios – Listagem de convênios

POST /disponibilidades – Cadastro de disponibilidades

GET /disponibilidades – Obtenção de horários para data e especialidade

POST /agendamentos – Agendamento de consulta

GET /agendamentos – Listagem de agendamentos

POST /atendimentos – Marcar como atendido

GET /atendimentos – Listagem de atendimentos
```

---

## Os requisitos para rodar o projeto

- Docker Engine e Docker Compose;
- (se for sem docker) .NET 8 SDK, se quiser executar o projeto sem container;
- (se for sem docker) Python, se quiser rodar o front sem o Docker.

---

## Rodar o projeto com docker compose

1. Só ir até a pagina do projeto:
   ```bash
   cd ~/Downloads/sistema_agendamento_testewest
   ```
2. Usar o comando para subir os serviços:
   ```bash
   docker compose up --build -d
   ```
3. Aplicar as migrations na primeira execução do projeto:
   ```bash
   docker compose exec api dotnet ef database update
   ```
4. O projeto vai ficar acessível:
   - **Swagger / API**: [http://localhost:5000](http://localhost:5000)
   - **Front-end** : [http://localhost:8000/index.html](http://localhost:8000/index.html)
5. Se quiser parar de rodar o projeto:
   ```bash
   docker compose down
   ```

## Execução Local

### Banco de Dados

- Vai ter que instalar e iniciar o PostgreSQL
- Criar o banco `AgendamentoDb` e configurar o usuário/senha
- Atualizar o `backend/appsettings.json` com uma nova connection string:
  ```json
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=AgendamentoDb;Username=postgres;Password=SUA_SENHA"
  }
  ```

### Back-end

No terminal:

```bash
cd backend
dotnet restore
dotnet ef database update
dotnet run
```

A API deve estar disponível em [http://localhost:5000](http://localhost:5000)

### Front-end

No terminal:

```bash
cd frontend
python3 -m http.server 8000
```

O front deve estar disponível em [http://localhost:8000/index.html](http://localhost:8000/index.html)
