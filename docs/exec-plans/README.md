# 执行计划和交接规范

本目录保存 agent 可继续执行的计划和交接状态。长期事实写入 `docs/design-docs/`、`docs/architecture.md` 或 `docs/decisions.md`；这里记录当前怎么做、做到哪里、如何验证。

## 目录结构

```text
docs/exec-plans/
  README.md
  active/
  completed/
  tech-debt-tracker.md
```

## Active handoff 格式

每个 active 文件应包含：

```md
# <里程碑名称>

## 状态（Status）

Not Started / In Progress / Blocked / Ready for Review

## 目标（Goal）

一到两句话说明本里程碑要交付什么。

## 范围（Scope）

- 本次要做的事项。
- 明确不做的事项。

## 当前进度（Progress）

- 已完成什么。
- 正在做什么。

## 下一步（Next Steps）

1. 可直接执行的下一步。
2. 对应文件路径。
3. 预期结果。

## 验证（Verification）

```powershell
dotnet test ProjectContextCompiler.slnx
```

记录最近一次运行结果、时间和失败原因。

## 决策（Decisions）

只记录本里程碑内影响执行的决策。长期决策同步到 `docs/decisions.md`。

## 阻塞（Blockers）

无阻塞写“无”。
```

## Completed 规则

里程碑完成后，把 active 文件移动到 `completed/`，并补齐：

- 变更摘要。
- 验证结果。
- 未解决风险。
- 后续技术债链接。

