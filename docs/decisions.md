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
