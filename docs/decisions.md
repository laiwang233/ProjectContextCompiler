# 决策记录

本文记录长期有效的架构和流程决策。临时执行状态写入 `docs/exec-plans/active/`。

## 2026-06-04：采用 repo-local knowledge store

状态（Status）：Accepted

决策（Decision）：`AGENTS.md` 作为短入口地图，`docs/` 作为事实来源。产品规格、架构、运行、测试、决策和交接状态都必须在仓库内可发现。

原因（Reason）：OpenAI harness engineering 经验强调 agent 需要可导航、可验证、可维护的仓库内知识，而不是巨大的单文件说明或聊天记录。

影响（Consequence）：新增长期约束时必须更新对应 docs；发现文档漂移时应补文档新鲜度测试。

## 2026-06-04：人工审核 Gate 是硬边界

状态（Status）：Accepted

决策（Decision）：LLM / Codex 只能生成候选 ContentBlock、Claim、Requirement 和 Task draft。`Requirement` 必须经人工审核成为 Approved 后，才能生成可执行任务和 Symphony Markdown。

原因（Reason）：本产品定位是 evidence-grounded、human-gated requirement compiler。跳过人工审核会让候选需求变成未经承诺的工程任务。

影响（Consequence）：任何绕过 `RequirementStatus.Approved`、`RequirementVersion` 绑定或证据引用的实现都应视为缺陷。

## 2026-06-05：ContentBlock 证据核验是 Claim 抽取前硬 Gate

状态（Status）：Accepted

决策（Decision）：ContentBlock 不再只是诊断信息，而是工作流中的“证据核验”主阶段。每个 ContentBlock 必须被人工核验为 `Confirmed` 或 `Ignored`；只有项目内全部 ContentBlock 已处理且至少存在一个 `Confirmed` 块时，才能抽取 Claim。Claim 抽取只使用 `Confirmed` ContentBlock。

原因（Reason）：产品定位是 evidence-grounded requirement compiler。若证据块未经显式确认就进入 Claim 抽取，后续 Requirement 和 Task 的证据链会看似完整但实际未经人工承诺。

影响（Consequence）：ContentBlock 持久化 `VerificationStatus`、`ReviewedBy` 和 `ReviewedAt`；旧数据默认为 `Pending`。前端工作台固定包含资料、证据核验、Claim、需求审核、任务、导出六阶段。未全部核验时 Claim 抽取返回 `ContentBlocksNotVerified`；全部忽略时返回 `NoConfirmedContentBlocks`。

## 2026-06-04：Mock provider 优先于真实 agent 执行

状态（Status）：Accepted

决策（Decision）：本地开发和测试默认使用 `MockStructuredGenerationProvider`，先保证端到端流程可重复，再切换 `LocalCodexCliProvider`。

原因（Reason）：Mock provider 让核心状态机、schema validation、review gate 和导出规则不依赖外部模型稳定性。

影响（Consequence）：测试优先覆盖 Mock 主流程；Local Codex provider 的职责收敛在独立 run 目录、文件 contract 和输出校验。

## 2026-06-04：GitHub control plane 暂不启用

状态（Status）：Accepted

决策（Decision）：当前仓库已有 Git 和 `origin` 远端，但本阶段只完成文档 repo 化，不启用 issue labels、agent assignee、branch、push 或 PR 自动化。

原因（Reason）：harness repo 化的第一步是建立文档事实来源。GitHub control plane 还需要 labels、issue 模板、CI gate 和 PR 终态约定，应该作为下一阶段单独验证。

影响（Consequence）：`AGENTS.md` 明确要求先用 `git status --short` 确认工作树状态；接入 NetSymphony 前需要人确认 labels、assignee、默认分支和 PR 策略。

## 2026-06-04：前端 UI 修改必须遵守后台工作台边界

状态（Status）：Accepted

决策（Decision）：所有涉及 `frontend/` 可见界面、布局、样式、交互状态或界面文案的修改，都必须遵守企业后台工作台定位。长期边界维护在 `docs/architecture.md` 的前端结构说明和本文的 Ant Design 工作流后台决策中，不再维护单独的前端 UI 约束文档。

原因（Reason）：Project Context Compiler 的前端定位是企业内部业务系统，而不是营销官网、SaaS Landing Page 或 AI 模板化宣传页。把 UI 约束收敛到架构和决策记录，可以减少文档入口分散，同时保留稳定、可执行的设计边界。

影响（Consequence）：`AGENTS.md` 只保留短入口；前端新增 UI 优先复用 Ant Design 核心组件、主题 token 和企业后台式交互模式。前端 UI 变更完成前仍需运行 `cd frontend && npm.cmd run build`，复杂视觉变更还应做浏览器冒烟检查。

## 2026-06-04：采用 Ant Design 工作流后台和分页列表 API

状态（Status）：Accepted

决策（Decision）：前端采用 Ant Design 作为后台组件体系；Tailwind CSS 只保留外层布局和响应式工具类；React Flow 从任务主视图降级为诊断页签。路由收敛为 `/projects` 和 `/projects/:projectId/workflow/:stageKey`，工作台主模型固定为资料、证据核验、Claim、需求审核、任务、导出六阶段。后端列表端点统一返回 `PagedResult<T>`，支持服务端分页、搜索、状态/类型筛选和白名单排序；`task-dependencies` 支持按 `taskId` 查询依赖邻域。

原因（Reason）：当前产品是企业内部工作流后台，主操作应该是高密度表格、明确状态、确认式审核和可分页列表，而不是自写样式工作台或默认加载全量数组。把列表查询能力放到后端，可以支撑几百到几千条数据，并让前端表格、筛选和分页状态稳定可控。

影响（Consequence）：前端新增 UI 优先使用 Ant Design 核心组件和 `ConfigProvider` theme token，不引入 ProComponents，不深度覆盖 `.ant-*` 内部样式。列表 API client 必须按 `PagedResult<T>` 消费数据；无效 `status`、`type` 或 `sortBy` 视为调用错误并返回 `400 InvalidQuery`。本轮只在 `PccDbContext` 增加常用索引配置，不引入 EF migrations。
