# 证据核验 Gate 和六阶段工作台收口

## 状态（Status）

Ready for Review

## 目标（Goal）

把 ContentBlock 证据核验提升为 Claim 抽取前硬 Gate，并将前端收敛为 Ant Design 六阶段工作台。同步补齐分页列表 API、测试覆盖、静态前端产物和 Stitch 原型资料。

## 范围（Scope）

- 后端新增 ContentBlock 核验状态、单条/批量核验 API、Claim 抽取前 Gate、分页列表查询契约和任务依赖邻域查询。
- 前端改为 `/projects` 与 `/projects/:projectId/workflow/:stageKey` 工作台路由，接入 Ant Design、分页表格、证据核验、详情 Drawer、审核动作和诊断页签。
- 文档同步架构、决策、测试入口、技术债跟踪和 PCC Flow v1.1 Stitch 原型。
- 不引入 EF migrations，不启用登录系统，不把本地浏览器 profile 或临时 smoke artifacts 纳入产品提交。

## 当前进度（Progress）

- 领域和业务服务已实现证据核验 Gate：所有 ContentBlock 必须核验为 `Confirmed` 或 `Ignored`，且至少一个 `Confirmed` 块才能抽取 Claim。
- `Requirement` 人工审核、Approved Requirement 任务生成、Symphony Markdown 导出硬规则保持有效。
- 列表端点已统一返回 `PagedResult<T>`，支持分页、搜索、状态/类型筛选和白名单排序；无效查询返回 `InvalidQuery`。
- 前端已按 Ant Design 工作流后台实现六阶段主界面，并同步构建产物到 `backend/src/Pcc.Api/wwwroot`。
- Stitch 原型和本地截图 bundle 已放入 `docs/prototypes/`，作为后续 UI 走查参考，不替代运行时实现。

## 验证（Verification）

最近验证：

```powershell
dotnet test ProjectContextCompiler.slnx
```

结果：通过，12 passed，0 failed。首次在沙箱内因 NuGet 源 TLS/凭证失败，提升权限完成 restore 后通过。

```powershell
cd frontend
npm.cmd run build
```

结果：通过。Vite 报告 chunk size warning，当前 `docs/testing.md` 已记录该 warning 不阻塞本轮验证。

## 决策（Decisions）

- ContentBlock 证据核验是 Claim 抽取前硬 Gate，长期决策记录在 `docs/decisions.md`。
- 前端采用 Ant Design 企业后台模式，Tailwind CSS 只作为外层布局和响应式工具类。
- 静态前端继续由 `Pcc.Api` 的 `wwwroot` 承载，前端 build 后需要同步产物。
- 本轮只增加常用 EF Core 索引配置，不引入 migrations。

## 未解决风险（Risks）

- 尚未做完整浏览器手工冒烟复核；后续 PR review 前建议按 `docs/testing.md` 的窄屏和六阶段清单走查一次。
- 前端 bundle 仍有 Vite 大 chunk warning；当前不影响功能，但后续可通过路由级拆包或组件懒加载优化。
- 架构依赖方向测试仍是技术债，记录在 `docs/exec-plans/tech-debt-tracker.md`。
