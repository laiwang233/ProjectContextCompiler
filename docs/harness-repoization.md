# Harness Repo 化指南

本文说明如何把 Project Context Compiler 整理成适合 Codex / NetSymphony 稳定编排的 harness repo。目标不是增加流程负担，而是把 agent 需要的事实来源（Source of Truth）、验证命令（Verification）和交接状态（Handoff）放进 repo-local 文档。

参考口径：

- NetSymphony 的 `AGENTS.md`、`docs/README.md`、`docs/exec-plans/` 和文档新鲜度测试模式。
- OpenAI《Harness engineering: leveraging Codex in an agent-first world》提出的原则：短 `AGENTS.md` 作为目录，不做百科；`docs/` 是 repository knowledge system of record；通过可机械检查的结构和验证命令让 agent 能自助导航、验证和收口。

## 目标状态（Target State）

当前仓库至少应包含：

- `AGENTS.md`：agent 入口地图，只写阅读顺序、语言规范、事实来源、硬规则和验证要求。
- `README.md`：人类入口，保留最短运行路径，并链接到 docs 索引。
- `docs/README.md`：repo-local knowledge store 索引。
- `docs/design-docs/`：长期产品和架构设计规格。
- `docs/exec-plans/`：active/completed 执行计划、交接状态和技术债。
- `docs/testing.md`：可复制执行的验证命令。
- `docs/operations.md`：本地运行、配置、端口和常见入口。
- `docs/troubleshooting.md`：已知环境差异和失败处理。
- `docs/decisions.md`：长期架构和流程决策。

## 当前 repo 化范围（Current Scope）

第一阶段只做文档 repo 化：

1. 新增短 `AGENTS.md`，把 agent 入口、硬闸门和验证命令放到固定位置。
2. 新增 `docs/` 知识库骨架，拆分架构、运行、测试、决策和故障排查。
3. 从 `MVP_Spec.md` 提炼一份短设计摘要，避免 agent 每次都读完整长文。
4. 建立 `docs/exec-plans/` 约定，让后续里程碑有 active handoff 和 completed archive。
5. 更新 `README.md`，让人类入口可以跳转到 harness 文档。

本阶段不改业务代码、不引入 GitHub control plane、不新增 CI。原因是当前目标是先固化 repo-local 事实来源；issue labels、agent assignee、branch、push、draft PR 和 CI gate 需要作为下一阶段单独验证。

## GitHub Control Plane

如果后续接入 NetSymphony / GitHub control plane，建议准备这些 labels：

```text
agent:ready
agent:running
agent:review
agent:failed
agent:blocked

priority:high
priority:normal
priority:low
```

首个可运行 issue 必须包含 `Acceptance Criteria`，并 assigned 给配置里的 agent 用户。成功终态是 `agent:review` 加 draft PR；是否可 merge 由 CI 绿且人工 review 无阻塞判断。

## Agent 可读性规则（Agent Legibility）

- `AGENTS.md` 是目录，不是手册。超过一屏的细节应移动到 `docs/`。
- 事实必须 repo-local。不要把关键规则只放在聊天、个人记忆或临时日志里。
- 文档要能指导下一步。每个执行计划必须包含当前状态、下一步、验证命令、阻塞和决策。
- 约束优先机械化。重复出现的文档漂移、分层漂移、状态机违规，应补测试或脚本，而不是只补一句提醒。
- 文档过时也是缺陷。发现代码和文档冲突时，当前变更要一起修正。

## 后续 repo 化里程碑（Follow-up Milestones）

1. 扩展文档新鲜度测试，让它覆盖新增 canonical docs 和 README 入口。
2. 新增架构测试，校验 `Domain`、`Application`、`Infrastructure`、`Api` 的依赖方向。
3. 为 GitHub Actions 或本地脚本建立统一验证入口。
4. 接入 NetSymphony control plane 后，用一个小型 issue 试运行从 `agent:ready` 到 draft PR 的闭环。
