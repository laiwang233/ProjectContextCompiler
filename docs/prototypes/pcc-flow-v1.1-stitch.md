# PCC Flow v1.1 Stitch 全流程原型

## 状态（Status）

- 日期：2026-06-08
- Stitch 项目：`projects/745781432599346214`
- 设计系统：`Systematic Density`
- 原型范围：桌面端后台，`/projects` + 工作台总览 + 六阶段工作流。
- 本地设计稿 bundle：[pcc-flow-v1.1-stitch/](pcc-flow-v1.1-stitch/README.md)
- 本地总览截图：[pcc-flow-v1.1-stitch/screens/contact-sheet.png](pcc-flow-v1.1-stitch/screens/contact-sheet.png)

本原型用于后续 agent 理解 PCC MVP 的后台工作流、页面信息密度和关键 Gate。它不是可运行前端实现，不替代 API、领域模型或测试。

## 事实来源（Source of Truth）

- 产品硬规则以 [`../../MVP_Spec.md`](../../MVP_Spec.md) 为准。
- 工程结构、前端路由和 Ant Design 工作台边界以 [`../architecture.md`](../architecture.md) 为准。
- 长期架构和前端取舍以 [`../decisions.md`](../decisions.md) 为准。
- 本文只记录当前 Stitch 原型中的页面、对象故事线和 UI 走查口径。

## 页面映射（Screen Map）

| Stitch screen | Screen id | 对应路由/阶段 | 本地设计稿 | 说明 |
| --- | --- | --- | --- | --- |
| `PCC Flow v1.1 - 项目列表` | `9f1167b7a714489f8ce943ebd5391012` | `/projects` | [01-projects.png](pcc-flow-v1.1-stitch/screens/01-projects.png) | 项目级入口。展示项目表格、最近活动和六阶段状态摘要；不强制显示单项目 Stepper。 |
| `PCC Flow v1.1 - 工作台总览` | `dfaf9b46fa0143a9b2a4eed8896c36b7` | `/projects/:projectId/workflow/overview` | [02-overview.png](pcc-flow-v1.1-stitch/screens/02-overview.png) | 项目仪表盘。展示当前 Gate、下一步和六阶段进度摘要。 |
| `PCC Flow v1.1 - 资料` | `1d8a806ac9d24705af9473a70fab2450` | `/projects/:projectId/workflow/artifacts` | [03-materials.png](pcc-flow-v1.1-stitch/screens/03-materials.png) | Artifact 管理、上传/粘贴入口、读取状态和诊断区。 |
| `PCC Flow v1.1 - 证据核验` | `5bed4c0c141348a38f3a2ab67cc3977c` | `/projects/:projectId/workflow/evidence` | [04-evidence.png](pcc-flow-v1.1-stitch/screens/04-evidence.png) | ContentBlock 表格、详情、确认/忽略操作，是 Claim 抽取前 Gate。 |
| `PCC Flow v1.1 - Claim` | `92c0a9c9a86242dbaa66f8d95b380afb` | `/projects/:projectId/workflow/claims` | [05-claim.png](pcc-flow-v1.1-stitch/screens/05-claim.png) | Claim 表格、证据引用和“生成候选 Requirement”入口。 |
| `PCC Flow v1.1 - 需求审核` | `108645a102394d45b6a4498474530176` | `/projects/:projectId/workflow/requirements` | [06-review.png](pcc-flow-v1.1-stitch/screens/06-review.png) | 人工审核 Candidate Requirement。审批前 `REQ-001 / v1 / PendingReview`。 |
| `PCC Flow v1.1 - 任务` | `9710f3b66d0f4b388450bd1e4f338886` | `/projects/:projectId/workflow/tasks` | [07-tasks.png](pcc-flow-v1.1-stitch/screens/07-tasks.png) | 从 Approved Requirement 生成任务，展示任务表和依赖诊断。 |
| `PCC Flow v1.1 - 导出 (修正布局)` | `803665c3844546d5abfe237e8d89eebc` | `/projects/:projectId/workflow/exports` | [08-export.png](pcc-flow-v1.1-stitch/screens/08-export.png) | Symphony Markdown Preview、Export Readiness、Included/Excluded、校验清单、历史导出和诊断。 |

程序化读取页面映射时使用 [manifest.json](pcc-flow-v1.1-stitch/manifest.json)。

## 统一故事线（Demo Story）

原型统一使用“成员邀请”需求作为主线：

- `REQ-001`：成员邀请主需求。
- 需求审核页展示审批前状态：`REQ-001 / v1 / PendingReview`。
- 任务页和导出页展示审批后状态：`REQ-001 / v1 / Approved`。
- `TASK-001` 到 `TASK-005` 全部来自 `REQ-001 v1`。
- `ModelRun` 不单独成页，只在相关页面的诊断区或运行记录中表达。

## Gate 规则（Gate Rules）

- LLM / Codex 输出只能作为候选内容，不能直接成为已批准需求。
- 所有 LLM 合成的 `Requirement` 初始状态必须是 `PendingReview`。
- `PendingReview`、`Rejected`、`Deferred` Requirement 不能生成 Implementation、Test 或 Migration 任务。
- 任务必须绑定 `RequirementId` 和 `RequirementVersionId`。
- Symphony Markdown 只能导出来自 Approved Requirement 的任务。
- Claim 抽取前必须完成 ContentBlock 证据核验；Ignored 块不参与 Claim 抽取。

## UI 口径（UI Guidance）

- 使用浅色 Ant Design 企业后台风格，紧凑但不要碎片化。
- 项目列表页是 `/projects` 入口例外，不强放单项目六阶段 Stepper。
- 工作台阶段页使用顶部项目 Header + 水平六阶段 Stepper。
- 六阶段中文名称保持：`资料`、`证据核验`、`Claim`、`需求审核`、`任务`、`导出`。
- 避免左侧营销导航、hero、插画、装饰性渐变、玻璃拟态和夸张阴影。
- 表格、状态 Tag、Tabs、按钮优先复用 Ant Design 核心组件和紧凑密度 token。
- 不要把卡片嵌套成多层卡片；详情、诊断和 Gate 提示应保持清晰分组。

## 已知展示风险（Known Risks）

- `需求审核` 页右侧动作区在截图中仍有轻微截断风险。后续实现时五个动作必须完整显示：`Approve`、`ApproveWithAssumptions`、`RequestClarification`、`Reject`、`Defer`。
- 导出页当前 Stitch 标题保留为 `PCC Flow v1.1 - 导出 (修正布局)`；这是当前保留版本，内容上作为导出阶段原型使用。
- 原型中的统计数字只用于走查，不作为数据契约；关键对象 ID、状态和 Gate 才是本页要保留的事实。

## 后续实现提示（Implementation Notes）

- 实现前先确认 `frontend/src/App.tsx` 当前路由和 `frontend/src/api/client.ts` API 契约。
- 前端不复制后端状态机；按钮禁用、Gate 提示和导出过滤应由 API 数据和后端规则驱动。
- 需求审核页应优先还原 V2.1 的视觉节奏：左侧表格 + 右侧详情/动作区，避免 V3 式过密碎片布局。
- 任务页和导出页必须在 UI 上明确显示任务来源为 `REQ-001 / v1 / Approved`，避免误导用户以为 PendingReview 可派生任务。
