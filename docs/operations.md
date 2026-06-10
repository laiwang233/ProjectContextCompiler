# 运行操作

本文记录本地运行 Project Context Compiler 的常用入口。

## 快速启动（InMemory）

不需要 Docker 的最快路径：

```powershell
$env:Database__Provider="InMemory"
dotnet run --project backend/src/Pcc.Api/Pcc.Api.csproj
```

打开：

```text
http://127.0.0.1:5088
```

API 文档：

```text
http://127.0.0.1:5088/scalar/v1
http://127.0.0.1:5088/swagger
```

## PostgreSQL 启动

```powershell
docker compose up -d
$env:Database__Provider="Postgres"
dotnet run --project backend/src/Pcc.Api/Pcc.Api.csproj
```

默认连接：

```text
Host=localhost;Port=5432;Database=pcc;Username=pcc;Password=pcc
```

## 前端 Vite 开发服务器

```powershell
cd frontend
npm.cmd install --cache .npm-cache --prefer-offline --no-audit --progress=false
npm.cmd run dev
```

打开：

```text
http://127.0.0.1:5173
```

## 端口冲突

如果 `5088` 被占用，可以直接打开已运行实例，或指定新端口：

```powershell
dotnet run --project backend/src/Pcc.Api/Pcc.Api.csproj --urls http://127.0.0.1:5090
```

## 结构化生成 Provider

默认 provider 是 `Mock`，适合本地开发和测试。

切换到本地 Codex CLI：

```powershell
$env:Generation__Provider="LocalCodexCli"
$env:LocalCodexCli__ExecutablePath="codex"
$env:LocalCodexCli__WorkingRoot="storage/model-runs"
dotnet run --project backend/src/Pcc.Api/Pcc.Api.csproj
```

Local Codex provider 只能通过独立 run 目录和 `output.json` 返回结构化 JSON，不允许直接访问数据库或修改业务状态。

