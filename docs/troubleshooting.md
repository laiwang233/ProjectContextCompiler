# 故障排查

## Git 状态无法解析

症状：

```text
fatal: not a git repository (or any of the parent directories): .git
```

处理：

- 先确认当前 shell 的工作目录是否是仓库根目录。
- 回到 `C:\Users\laiwang\Documents\project_csharp\ProjectContextCompiler` 后再运行 `git status --short`。
- 不要在 Git 状态不明确时 commit、push 或创建 PR。

## 中文文档在 PowerShell 中显示乱码

症状：中文 Markdown 读取后出现乱码。

处理：

```powershell
Get-Content -Raw -Encoding utf8 -LiteralPath 'MVP_Spec.md'
```

新增或编辑中文文档时保持 UTF-8。

## API 端口被占用

症状：`dotnet run` 报端口占用，或浏览器打不开预期服务。

处理：

```powershell
dotnet run --project backend/src/Pcc.Api/Pcc.Api.csproj --urls http://127.0.0.1:5090
```

然后打开：

```text
http://127.0.0.1:5090
```

## PostgreSQL 无法连接

先确认 Docker 服务：

```powershell
docker compose ps
```

如果只需要本地开发闭环，切换 InMemory：

```powershell
$env:Database__Provider="InMemory"
dotnet run --project backend/src/Pcc.Api/Pcc.Api.csproj
```

## 前端依赖缺失

症状：`npm.cmd run build` 或 `npm.cmd run dev` 找不到依赖。

处理：

```powershell
cd frontend
npm.cmd install --cache .npm-cache --prefer-offline --no-audit --progress=false
```

## Local Codex provider 没有输出

检查：

- `Generation:Provider` 是否为 `LocalCodexCli`。
- `LocalCodexCli:ExecutablePath` 是否能在当前 shell 中执行。
- run 目录中是否生成 `instructions.md`、`request.json`、`output.schema.json`。
- `output.json` 是否存在且是合法 JSON。
- schema validation 错误会记录为 failed ModelRun，业务数据不应落库。
