# 架构

Project Context Compiler 是一个 LLM-first、evidence-grounded、human-gated requirement compiler。核心流程来自 `MVP_Spec.md`：

```text
资料 -> ContentBlock 证据核验 -> Claim -> 候选 Requirement -> 人工审核 -> Approved Requirement -> Task DAG -> Symphony Markdown
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

前端位于 `frontend/`，使用 React、Vite、TypeScript、TanStack Query、Ant Design、Tailwind CSS 和 React Flow。

关键入口：

```text
frontend/src/main.tsx
frontend/src/App.tsx
frontend/src/api/client.ts
frontend/src/types.ts
```

Ant Design 是可见组件和交互状态的主体系，`ConfigProvider` 在根组件配置后台主题、紧凑密度、圆角和主色。Tailwind CSS 只作为外层布局和响应式工具类，不深度覆盖 `.ant-*` 组件内部样式。React Flow 只用于任务详情诊断视图，不作为任务列表主入口。

当前前端路由直接对应工作流后台：

```text
/projects
/projects/:projectId/workflow/:stageKey
```

`/projects` 是项目列表页；工作台包含资料、证据核验、Claim、需求审核、任务、导出六个阶段。证据核验阶段是 Claim 抽取前的主流程 Gate，用于展示 ContentBlock 表格、详情、状态筛选、批量确认和批量忽略；模型运行记录保留在详情区诊断页签。

当前六阶段 `stageKey` 分别为 `artifacts`、`evidence`、`claims`、`requirements`、`tasks`、`exports`。旧的 section 路由不保留兼容跳转。

前端通过 API client 调用后端，不直接复制后端状态机和导出规则。`Pcc.Api` 从 `backend/src/Pcc.Api/wwwroot` 承载静态前端产物；更新前端后需要把 `frontend/dist` 同步到该目录。

## 列表 API 契约（List API Contract）

以下列表端点返回通用分页对象 `PagedResult<T>`：

```text
GET /api/projects
GET /api/projects/{projectId}/artifacts
GET /api/projects/{projectId}/content-blocks
GET /api/projects/{projectId}/claims
GET /api/projects/{projectId}/requirements
GET /api/projects/{projectId}/tasks
GET /api/projects/{projectId}/exports
GET /api/projects/{projectId}/model-runs
```

分页对象字段：

```text
items
totalCount
pageNumber
pageSize
```

统一查询参数：`pageNumber`、`pageSize`、`q`、`status`、`type`、`sortBy`、`sortDirection`。默认 `pageNumber=1`、`pageSize=20`，最大 `pageSize=100`。`status`、`type` 和 `sortBy` 必须通过各端点白名单校验；无效参数返回 `400` 和 `InvalidQuery` 错误。`GET /api/projects/{projectId}/content-blocks` 的 `status` 使用 `ContentBlock.VerificationStatus`，即 `Pending`、`Confirmed`、`Ignored`。`task-dependencies` 支持可选 `taskId`，用于查询选中任务的依赖邻域，避免默认加载全量 DAG。

ContentBlock 核验操作：

```text
PUT /api/content-blocks/{contentBlockId}/review
PUT /api/projects/{projectId}/content-blocks/review
```

当前没有登录系统，核验人沿用人工审核默认值 `PM`。旧 ContentBlock 的 `VerificationStatus` 默认为 `Pending`。

## 核心边界（Core Contracts）

- 结构化生成只能通过 `IStructuredGenerationProvider`。
- Mock provider 用于本地端到端闭环和测试。
- Local Codex provider 必须创建独立 run 目录，写入 `instructions.md`、`request.json`、`output.schema.json`，并只从 `output.json` 读取结果。
- Provider 输出必须 schema validation；校验失败不能落业务数据。
- Claim 抽取前必须处理项目内全部 ContentBlock，且至少有一个 `Confirmed` ContentBlock；抽取只使用 `Confirmed` 块。
- Requirement 审核必须绑定具体 `RequirementVersion`。
- Task 和 Export 必须绑定 Approved Requirement 及其 version。

## 需要机械化的约束（Mechanical Constraints）

当前仓库还没有架构测试。后续应补测试覆盖：

- `Pcc.Domain` 不依赖 `Pcc.Application`、`Pcc.Infrastructure` 或 `Pcc.Api`。
- `Pcc.Application` 不依赖 `Pcc.Infrastructure` 或 `Pcc.Api`。
- `Pcc.Infrastructure` 不依赖 `Pcc.Api`。
- `AGENTS.md` 和 `docs/README.md` 引用的关键文档存在。
- `docs/superpowers/` 不作为长期事实来源。

