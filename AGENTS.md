# AGENTS.md

本文件是 Project Context Compiler 的 agent 入口地图。保持短小、稳定、可执行；不要把长篇设计、临时进度或一次性发现写在这里。

## 阅读顺序

1. 先读本文件，确认语言、文档、工作边界和验证要求。
2. 需要理解产品目标时，读 `MVP_Spec.md`，再读 `docs/design-docs/index.md`。
3. 需要理解工程结构时，读 `docs/architecture.md`。
4. 涉及前端 UI、布局、样式、交互状态或界面文案修改时，在读取或修改 `frontend/` 代码前必须先读 `docs/architecture.md` 的前端结构说明和 `docs/decisions.md` 的 Ant Design 工作流后台决策。
5. 需要运行或验证时，读 `docs/operations.md` 和 `docs/testing.md`。
6. 需要继续当前里程碑时，读 `docs/exec-plans/README.md`，再读 `docs/exec-plans/active/` 中的对应交接文件。
7. 需要理解长期约束和历史取舍时，读 `docs/decisions.md`。

## 语言规范

- 默认使用中文回复、编写规范文档、设计文档、执行计划、交接文件、README 和 commit message。
- 项目内人类可读或 agent-readable 文本默认使用中文，包括 prompt、错误说明、测试 fixture、PR body 和运行报告。
- 专业术语、产品名、库名、API 名、协议名、命令、路径、分支名、配置键、代码标识符和 HTTP endpoint 保留对应语言或项目惯例。
- Harness-style 文档章节使用“中文名称（English alias）”格式，例如“状态（Status）”“事实来源（Source of Truth）”“验证（Verification）”。

## 事实来源

- `MVP_Spec.md`：产品范围、领域模型、硬闸门和分阶段实现的原始规格。
- `docs/README.md`：repo-local knowledge store 索引。
- `docs/design-docs/`：长期产品和架构设计规格。
- `docs/architecture.md`：工程结构、前端组件体系、静态承载方式和列表 API 契约。
- `docs/decisions.md`：长期架构、流程和前端后台工作台取舍。
- `docs/exec-plans/active/`：当前执行计划、进度、决策和交接状态。
- `docs/exec-plans/completed/`：已完成并收口的执行计划。
- `docs/exec-plans/tech-debt-tracker.md`：执行中发现但不属于当前里程碑的后续事项。
- `README.md`：人类入口和最短运行说明。

## 工程边界

- 保持 `Pcc.Domain -> Pcc.Application -> Pcc.Infrastructure -> Pcc.Api` 的依赖方向。
- `Pcc.Domain` 只放实体、枚举、领域规则和领域异常；不要依赖 EF Core、HTTP、filesystem、Codex CLI 或前端概念。
- `Pcc.Application` 只暴露 DTO、use case 接口和 provider contract；不要依赖 `Pcc.Infrastructure`。
- `Pcc.Infrastructure` 实现 EF Core、文件存储、结构化生成 provider、导出和业务服务编排。
- `Pcc.Api` 只负责 hosting、DI、Minimal API endpoint、Swagger/Scalar 和静态前端承载。
- 前端代码保持在 `frontend/` 内，通过 `frontend/src/api/client.ts` 调后端 API，不直接复制后端业务规则。
- 所有前端 UI 修改必须遵守 Ant Design 工作流后台边界，优先复用 AntD 核心组件、主题 token 和企业后台式交互模式。

## 产品硬规则

- LLM / Codex 输出只能作为候选内容，不能直接成为已批准需求。
- 所有 LLM 合成的 `Requirement` 初始状态必须是 `PendingReview`。
- 未审核或未批准的 `Requirement` 不能生成 Implementation、Test 或 Migration 任务。
- 可执行任务必须绑定 `RequirementId` 和 `RequirementVersionId`。
- Symphony Markdown 只能导出来自 Approved Requirement 的任务。
- Provider 输出必须先通过 JSON parse 和 schema validation，再进入业务数据。
- `LocalCodexCliProvider` 只能通过独立 run 目录和 `output.json` 返回结果；不能直接访问数据库、修改业务状态或导出任务。

## 验证要求

完成后端或领域改动前，至少运行：

```powershell
dotnet test ProjectContextCompiler.slnx
```

完成前端改动前，至少运行：

```powershell
cd frontend
npm.cmd run build
```

只改文档时，至少运行文档新鲜度测试：

```powershell
dotnet test ProjectContextCompiler.slnx --filter FullyQualifiedName~DocumentationFreshnessTests
```

## Git 规范

当前仓库使用 Git 管理，默认远端为 `origin`，当前稳定分支是 `master`。不要在用户未要求时自动 commit、push 或创建 PR；执行任何 Git 写操作前先用 `git status --short` 确认工作树状态。

如果需要提交 commit，使用中文 Conventional Commits：

- `type(scope)` 使用 Conventional Commits 约定，例如 `docs(agents)`、`docs(harness)`、`docs(exec-plans)`、`feat(workflow)`、`test(workflow)`、`test(docs)`。
- subject 描述具体变更，不写空泛模板。
- commit body 说明变更原因、主要内容、验证结果和风险。
- `master` 是当前稳定分支。新里程碑默认从最新 `master` 创建里程碑分支；分支无法使用 `codex/` 前缀时，使用不冲突但语义等价的里程碑分支名。
- 里程碑完成且验证通过后，默认按顺序执行：提交中文 Conventional Commit、push 当前里程碑分支到 `origin`、创建指向 `master` 的 draft Pull Request；除非用户明确要求暂不提交、暂不 push 或暂不创建 PR。
- 如果当前工作直接发生在 `master`，不要直接创建 PR；完成验证和 commit 前，先说明当前分支边界并等待用户确认是否需要补建里程碑分支。
- 如果工作树包含用户或其他工具产生的无关改动，不要回滚；提交前只 stage 当前任务相关文件，并在最终说明中列出未纳入的改动。
- 提交前必须运行当前任务要求的验证命令；验证失败时不要 commit，先修复或明确记录阻塞。

## 文档

- 当面向用户的行为发生变化时，需要检查是否需要同步更新文档、示例或变更日志。
- 公开文档中只能包含公开信息，或本仓库中可见的行为说明。
- 保留现有术语和 frontmatter。
- 在最终交付前，运行文档格式化和构建检查。