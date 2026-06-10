# Project Context Compiler

MVP implementation for `MVP_Spec.md`.

## Repository harness（仓库 harness）

本仓库把 agent-facing 知识放在 repo-local 文档中：

- [AGENTS.md](AGENTS.md) 是短 agent 入口地图。
- [docs/README.md](docs/README.md) 是文档索引。
- [docs/harness-repoization.md](docs/harness-repoization.md) 说明 harness repo 结构和后续工作。
- [docs/architecture.md](docs/architecture.md)、[docs/operations.md](docs/operations.md)、[docs/testing.md](docs/testing.md) 是主要工程参考。

## Run

Fast local development path, no Docker required:

```powershell
$env:Database__Provider="InMemory"
dotnet run --project backend/src/Pcc.Api/Pcc.Api.csproj
```

Open `http://127.0.0.1:5088`.

Local API docs:

- Scalar: `http://127.0.0.1:5088/scalar/v1`
- Swagger UI: `http://127.0.0.1:5088/swagger`

To run with PostgreSQL:

```powershell
docker compose up -d
$env:Database__Provider="Postgres"
dotnet run --project backend/src/Pcc.Api/Pcc.Api.csproj
```

Optional Vite dev server:

```powershell
cd frontend
npm.cmd install --cache .npm-cache --prefer-offline --no-audit --progress=false
npm.cmd run dev
```

Open `http://127.0.0.1:5173`.

If port `5088` is already in use, either open `http://127.0.0.1:5088` because the API is already running, or run on another port:

```powershell
dotnet run --project backend/src/Pcc.Api/Pcc.Api.csproj --urls http://127.0.0.1:5090
```

## Verify

```powershell
dotnet test ProjectContextCompiler.slnx
cd frontend
npm.cmd run build
```

Default structured generation provider is `Mock`. Set `Generation:Provider` to `LocalCodexCli` to route through the local Codex CLI provider.
