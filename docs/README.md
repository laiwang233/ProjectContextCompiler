# Project Context Compiler 文档索引

本目录是 Project Context Compiler 的 repo-local knowledge store。`AGENTS.md` 只做入口地图；长期事实、设计、执行状态、验证命令和决策记录放在这里并随代码维护。

## 快速入口

- [Harness repo 化指南](harness-repoization.md)
- [架构](architecture.md)
- [运行操作](operations.md)
- [测试](testing.md)
- [决策记录](decisions.md)
- [故障排查](troubleshooting.md)

## 设计和执行

- [设计文档索引](design-docs/index.md)
- [Project Context Compiler MVP 设计摘要](design-docs/2026-06-04-project-context-compiler-mvp.md)
- [原型索引](prototypes/README.md)
- [执行计划和交接规范](exec-plans/README.md)
- [当前 active 执行目录](exec-plans/active/)
- [已完成执行目录](exec-plans/completed/)
- [技术债跟踪](exec-plans/tech-debt-tracker.md)

## 原始规格

- [`../MVP_Spec.md`](../MVP_Spec.md)：产品目标、数据模型、API、流程、测试和 Definition of Done 的原始规格。
- [`../README.md`](../README.md)：最短运行和验证说明。

## 维护规则

- 新增长期知识时优先更新本索引。
- 临时进度、阻塞和验证结果写入当前 active handoff。
- 已完成的执行交接移动到 `exec-plans/completed/`。
- 发现文档与代码不一致时，优先修正文档或补文档新鲜度测试，不把关键事实留在聊天记录里。

