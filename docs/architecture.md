# 架构

Project Context Compiler 是一个 LLM-first、evidence-grounded、human-gated requirement compiler。核心流程来自 `MVP_Spec.md`：

```text
资料 -> ContentBlock -> Claim -> 候选 Requirement -> 人工审核 -> Approved Requirement -> Task DAG -> Symphony Markdown
```

## 后端结构（Backend Structure）

当前 solution 包含四个生产项目和一个测试项目：

```text
backend/src/Pcc.Domain/Pcc.Domain.csproj
backend/src/Pcc.Application/Pcc.Application.csproj
backend/src/Pcc.Infrastructure/Pcc.Infrastructure.csproj
backend/src/Pcc.Api/Pcc.Api.csproj
backend/tests/Pcc.UnitTests/Pcc.UnitTests.csproj
```

## 分层边界（Layer Boundaries）

- `Pcc.Domain`：实体、枚举、领域规则、领域异常和 `RequirementContent`。不依赖 hosting、HTTP、EF Core、filesystem、Codex CLI 或前端概念。
- `Pcc.Application`：DTO、`ICompilerWorkflow` 和 `IStructuredGenerationProvider` contract。只依赖 `Domain`。
- `Pcc.Infrastructure`：EF Core `PccDbContext`、文件存储、Mock / Local Codex provider、JSON schema validation、Symphony Markdown exporter 和 `CompilerWorkflowService`。
- `Pcc.Api`：ASP.NET Core hosting、DI、Minimal API endpoint、Swagger、Scalar 和静态前端承载。
- `Pcc.UnitTests`：覆盖核心 workflow、人工审核 gate、导出过滤和领域状态流转。

## 前端结构（Frontend Structure）

前端位于 `frontend/`，使用 React、Vite、TypeScript、TanStack Query、Tailwind CSS 和 React Flow。

关键入口：

```text
frontend/src/main.tsx
frontend/src/App.tsx
frontend/src/api/client.ts
frontend/src/types.ts
```

前端通过 API client 调用后端，不直接复制后端状态机和导出规则。

## 核心边界（Core Contracts）

- 结构化生成只能通过 `IStructuredGenerationProvider`。
- Mock provider 用于本地端到端闭环和测试。
- Local Codex provider 必须创建独立 run 目录，写入 `instructions.md`、`request.json`、`output.schema.json`，并只从 `output.json` 读取结果。
- Provider 输出必须 schema validation；校验失败不能落业务数据。
- Requirement 审核必须绑定具体 `RequirementVersion`。
- Task 和 Export 必须绑定 Approved Requirement 及其 version。

## 需要机械化的约束（Mechanical Constraints）

当前仓库还没有架构测试。后续应补测试覆盖：

- `Pcc.Domain` 不依赖 `Pcc.Application`、`Pcc.Infrastructure` 或 `Pcc.Api`。
- `Pcc.Application` 不依赖 `Pcc.Infrastructure` 或 `Pcc.Api`。
- `Pcc.Infrastructure` 不依赖 `Pcc.Api`。
- `AGENTS.md` 和 `docs/README.md` 引用的关键文档存在。
- `docs/superpowers/` 不作为长期事实来源。

