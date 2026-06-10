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
- Task 生成新增 `DeferredScope` 范围护栏：允许负向提及延期事项，但拒绝把延期范围生成可执行任务，并保持拒绝后不落库。
- 列表端点已统一返回 `PagedResult<T>`，支持分页、搜索、状态/类型筛选和白名单排序；无效查询返回 `InvalidQuery`。
- 前端已按 Ant Design 工作流后台实现六阶段主界面，并同步构建产物到 `backend/src/Pcc.Api/wwwroot`。
- PR review 前本地 InMemory 浏览器冒烟已完成：项目列表、新建项目、资料新增/读取、证据核验、Claim 抽取、需求审核、任务生成、Symphony Markdown 导出和诊断页签均通过；冒烟中发现并修复资料新增 Modal 保存后卡住的阻塞问题。
- 390px 窄屏六阶段检查已完成：各阶段无全页横向溢出，mobile stage select、详情入口和关键动作可见。
- Stitch 原型和本地截图 bundle 已放入 `docs/prototypes/`，作为后续 UI 走查参考，不替代运行时实现。

## 验证（Verification）

最近验证：

```powershell
dotnet test ProjectContextCompiler.slnx
```

结果：通过，19 passed，0 failed。首次在沙箱内因 NuGet 源 TLS/凭证失败，提升权限完成 restore 后通过；补充 DeferredScope 护栏后再次完整运行通过。

```powershell
dotnet test ProjectContextCompiler.slnx --filter FullyQualifiedName~EvidenceGroundedWorkflowEvalTests
```

结果：通过，7 passed，0 failed。覆盖 Confirmed evidence 上下文、人审 Gate、任务来源版本绑定、schema invalid 不落任务，以及 DeferredScope 正负向任务生成边界。

```powershell
cd frontend
npm.cmd run build
```

结果：通过。Vite 报告 chunk size warning，当前 `docs/testing.md` 已记录该 warning 不阻塞本轮验证。

```text
本地 InMemory 浏览器冒烟
```

结果：通过。覆盖项目列表、新建项目、资料新增和读取、证据核验批量确认、Claim 抽取、需求合成和批准、任务生成、Symphony Markdown 导出、导出详情、项目诊断页签，以及 390px 窄屏布局。冒烟中发现资料新增 Modal 在保存成功后仍保持打开和 loading，已改为显式 footer 按钮并验证通过。

## 决策（Decisions）

- ContentBlock 证据核验是 Claim 抽取前硬 Gate，长期决策记录在 `docs/decisions.md`。
- DeferredScope 不能被任务生成重新执行，长期决策记录在 `docs/decisions.md`。
- 前端采用 Ant Design 企业后台模式，Tailwind CSS 只作为外层布局和响应式工具类。
- 静态前端继续由 `Pcc.Api` 的 `wwwroot` 承载，前端 build 后需要同步产物。
- 本轮只增加常用 EF Core 索引配置，不引入 migrations。

## 未解决风险（Risks）

- 前端 bundle 仍有 Vite 大 chunk warning；当前不影响功能，但后续可通过路由级拆包或组件懒加载优化。
- 架构依赖方向测试仍是技术债，记录在 `docs/exec-plans/tech-debt-tracker.md`。
