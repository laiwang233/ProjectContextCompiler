# Project Context Compiler MVP 设计摘要

## 状态（Status）

Accepted。本文是 `MVP_Spec.md` 的 agent-readable 摘要，不替代原始规格。

## 目标（Goal）

Project Context Compiler 是一个 LLM-first、evidence-grounded、human-gated requirement compiler。用户上传或粘贴项目资料，系统抽取证据块和需求事实，合成候选需求，经人工审核后生成 agent-ready 任务，并导出 Symphony Markdown。

## 主流程（Workflow）

```text
Artifact Intake
  -> Artifact Reader
  -> ContentBlock Extraction
  -> ContentBlock Evidence Review Gate
  -> Claim Extraction
  -> Requirement Synthesis
  -> Requirement Review Gate
  -> Task Generation
  -> Symphony Markdown Export
```

## 前端工作台模型（Frontend Workbench）

当前前端采用 Ant Design 后台工作台结构，不保留旧 section 路由兼容跳转。

```text
/projects
/projects/:projectId/workflow/:stageKey
```

`/projects` 使用项目表格、搜索和新建项目弹窗。项目工作台按六个业务阶段组织：

```text
资料 -> 证据核验 -> Claim -> 需求审核 -> 任务 -> 导出
```

当前 `stageKey` 使用 `artifacts`、`evidence`、`claims`、`requirements`、`tasks`、`exports`。每个阶段主体采用“列表 + 详情”结构：桌面端右侧固定详情面板，窄屏下点击行打开 Drawer。证据核验阶段用于确认或忽略 ContentBlock，是 Claim 抽取前硬 Gate。Requirement 审核通过 Modal 输入评论并确认，不做行内直接提交。模型运行记录和任务 DAG 属于诊断信息，放在详情区诊断页签；任务列表默认使用分页表格，DAG 只展示选中任务邻域或受限节点数。

## 职责边界（Responsibility Boundary）

程序负责：

- 保存原始资料。
- 识别类型、轻量文本提取、切片。
- 调用 `IStructuredGenerationProvider`。
- JSON parse 和 schema validation。
- 落库、状态机、人工审核 gate、导出。

LLM / Codex 负责：

- 读取资料。
- 识别 ContentBlock。
- 抽取 Claim。
- 合成候选 Requirement。
- 生成候选 Task draft。

人负责：

- 审核、编辑、批准、拒绝、延期或要求澄清 Requirement。

## 硬规则（Hard Rules）

- `ContentBlock` 阶段只允许转写、摘录和结构化，不做需求判断。
- Claim 抽取前必须处理全部 `ContentBlock`，且至少存在一个 `Confirmed` 块；抽取只使用 `Confirmed` 块。
- `Claim` 必须引用至少一个 `ContentBlock`。
- LLM 合成的 `Requirement` 初始状态必须是 `PendingReview`。
- Requirement 编辑必须创建新的 `RequirementVersion`。
- Requirement 审核必须绑定具体 `RequirementVersion`。
- 只有 `Approved Requirement` 可以生成可执行任务。
- `OrchestrationTask` 必须绑定 `RequirementId` 和 `RequirementVersionId`。
- Export 只能包含来自 Approved Requirement 的任务。
- Provider 输出必须通过 JSON schema validation 后才能落库。
- `LocalCodexCliProvider` 不允许直接访问数据库、修改业务状态或导出任务。

## 列表 API（Paged List API）

列表端点返回 `PagedResult<T>`，字段为 `items`、`totalCount`、`pageNumber`、`pageSize`。默认 `pageNumber=1`、`pageSize=20`，最大 `pageSize=100`。

适用端点：

- `GET /api/projects`
- `GET /api/projects/{projectId}/artifacts`
- `GET /api/projects/{projectId}/content-blocks`
- `GET /api/projects/{projectId}/claims`
- `GET /api/projects/{projectId}/requirements`
- `GET /api/projects/{projectId}/tasks`
- `GET /api/projects/{projectId}/exports`
- `GET /api/projects/{projectId}/model-runs`

统一查询参数为 `pageNumber`、`pageSize`、`q`、`status`、`type`、`sortBy`、`sortDirection`。排序字段使用端点白名单；无效 `status`、`type` 或 `sortBy` 返回清晰 `400`。`task-dependencies` 支持按 `taskId` 查询邻域。

## 代码映射（Code Map）

- Domain：`backend/src/Pcc.Domain/Entities.cs`、`Enums.cs`、`RequirementContent.cs`、`DomainExceptions.cs`。
- Application：`backend/src/Pcc.Application/Services/ICompilerWorkflow.cs`、`Generation/StructuredGenerationContracts.cs`、`Dtos/WorkflowDtos.cs`。
- Infrastructure：`backend/src/Pcc.Infrastructure/Services/CompilerWorkflowService.cs`、`Generation/*`、`Persistence/PccDbContext.cs`、`Export/SymphonyMarkdownExporter.cs`。
- API：`backend/src/Pcc.Api/Program.cs`。
- Frontend：`frontend/src/App.tsx`、`frontend/src/api/client.ts`、`frontend/src/types.ts`。
- Tests：`backend/tests/Pcc.UnitTests/CompilerWorkflowTests.cs`。

## MVP 完成标准（Definition of Done）

最小完成闭环：

```text
创建项目
-> 上传或粘贴资料
-> 读取资料并生成 ContentBlock
-> 确认或忽略 ContentBlock
-> 从 Confirmed ContentBlock 抽取 Claim
-> 从 Claim 生成 PendingReview Requirement
-> 人工审核 Requirement
-> Approved Requirement 生成任务
-> 在任务详情诊断视图查看选中任务邻域 DAG
-> 导出 Symphony Markdown
```

验证标准：

- `dotnet test ProjectContextCompiler.slnx` 通过。
- `frontend` 的 `npm.cmd run build` 通过。
- 未审核需求不能生成可执行任务。
- 导出内容包含 `RequirementId`、`RequirementVersion`、证据、验收标准和 `Agent Prompt`。
- 所有模型调用都有 `ModelRun` 记录。

