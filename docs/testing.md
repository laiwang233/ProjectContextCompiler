# 测试

本文记录当前可复制执行的验证命令。执行命令前默认位于仓库根目录：

```powershell
C:\Users\laiwang\Documents\project_csharp\ProjectContextCompiler
```

## 后端验证（Backend Verification）

```powershell
dotnet test ProjectContextCompiler.slnx
```

当前后端测试入口：

- `backend/tests/Pcc.UnitTests/CompilerWorkflowTests.cs`：覆盖 Mock workflow、人工审核 gate、Approved 后生成任务、Symphony Markdown 导出、ModelRun 记录和 Requirement review 状态流转。

## 前端验证（Frontend Verification）

```powershell
cd frontend
npm.cmd run build
```

如依赖未安装：

```powershell
cd frontend
npm.cmd install --cache .npm-cache --prefer-offline --no-audit --progress=false
npm.cmd run build
```

## 手工冒烟（Manual Smoke）

本地 API：

```powershell
$env:Database__Provider="InMemory"
dotnet run --project backend/src/Pcc.Api/Pcc.Api.csproj
```

打开：

```text
http://127.0.0.1:5088
http://127.0.0.1:5088/scalar/v1
http://127.0.0.1:5088/swagger
```

可使用 `MVP_Spec.md` 第 23 节的会议记录示例，手工验证：

```text
创建项目 -> 粘贴资料 -> Read Artifact -> Extract Claims -> Synthesize Requirements -> Review -> Generate Tasks -> Export Symphony Markdown
```

## 文档验证（Documentation Verification）

文档新鲜度检查已经机械化，改动 agent-facing 文档时运行：

```powershell
dotnet test ProjectContextCompiler.slnx --filter FullyQualifiedName~DocumentationFreshnessTests
```

当前覆盖：

- `README.md` 能链接到 `docs/README.md`。
- `AGENTS.md` 中列出的关键文档存在。
- `docs/README.md` 中列出的链接存在。
- `docs/` 内 Markdown 相对链接可解析。
- `docs/superpowers/` 不作为长期事实来源。
- 新增长期规则时同步更新 `docs/decisions.md` 或对应设计文档。
