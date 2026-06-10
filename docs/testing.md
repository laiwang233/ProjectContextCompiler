# 测试

本文记录当前可复制执行的验证命令。执行命令前默认位于仓库根目录：

```powershell
C:\Users\laiwang\Documents\project_csharp\ProjectContextCompiler
```

## 后端验证（Backend Verification）

```powershell
dotnet test ProjectContextCompiler.slnx
```

当前后端测试入口：

- `backend/tests/Pcc.UnitTests/CompilerWorkflowTests.cs`：覆盖 Mock workflow、ContentBlock 默认待核验、确认/忽略和批量核验、Claim 抽取前证据核验 gate、Ignored 块不参与抽取、全忽略禁止抽取、人工审核 gate、Approved 后生成任务、Symphony Markdown 导出、ModelRun 记录、Requirement review 状态流转，以及列表 API 的分页上限、文本搜索、状态/类型筛选、排序白名单和无效查询参数。
- `backend/tests/Pcc.UnitTests/Evals/EvidenceGroundedWorkflowEvalTests.cs`：覆盖 eval-style workflow 回归场景，使用 scripted provider 验证 Confirmed evidence 上下文、人审 Gate、任务来源版本绑定、schema invalid 不落任务，以及 Task 生成不能把 `DeferredScope` 中的事项变成可执行工作。

单独运行 evidence-grounded eval：

```powershell
dotnet test ProjectContextCompiler.slnx --filter FullyQualifiedName~EvidenceGroundedWorkflowEvalTests
```

这组 eval 不调用真实 LLM，也不使用 promptfoo。它验证业务护栏和 provider contract 边界；真实模型输出质量、prompt 对比和 LLM-as-judge 后续再单独接入。

## 前端验证（Frontend Verification）

```powershell
cd frontend
npm.cmd run build
```

当前 Ant Design 工作台 build 可能触发 Vite chunk size warning；只要命令退出码成功，当前验证视为通过。后续如果需要优化首屏体积，再单独引入路由级拆包或组件级懒加载。

如依赖未安装：

```powershell
cd frontend
npm.cmd install --cache .npm-cache --prefer-offline --no-audit --progress=false
npm.cmd run build
```

## 手工冒烟（Manual Smoke）

本地 API：

```powershell
$env:Database__Provider="InMemory"
dotnet run --project backend/src/Pcc.Api/Pcc.Api.csproj
```

打开：

```text
http://127.0.0.1:5088
http://127.0.0.1:5088/scalar/v1
http://127.0.0.1:5088/swagger
```

可使用 `MVP_Spec.md` 第 23 节的会议记录示例，手工验证：

```text
创建项目 -> 粘贴资料 -> Read Artifact -> 证据核验 -> Extract Claims -> Synthesize Requirements -> Review -> Generate Tasks -> Export Symphony Markdown
```

前端工作台还应覆盖：

```text
项目列表 -> 新建项目 -> 六阶段切换 -> 证据块表格与详情 Drawer -> 批量确认/忽略 -> Claim Gate 按钮禁用提示 -> 审核 Modal -> 批准后任务来源预选 -> 诊断页签 -> 窄屏布局
```

窄屏布局检查至少使用 390px 宽度，覆盖：

```text
详情入口可见 -> 点击行或查看详情打开 Drawer -> 关键审核动作不只依赖横向滚动 -> 无全页横向溢出 -> 阶段切换控件可用 -> Gate 阻塞提示仍清楚
```

视觉走查还应确认：

```text
无营销式 hero -> 无装饰性渐变/玻璃拟态/夸张阴影 -> 阶段导航不过度解释功能 -> 常驻提示只在空态、错误或 Gate 阻塞时出现 -> 详情区不出现不必要的卡片套卡片
```

## 文档验证（Documentation Verification）

文档新鲜度检查已经机械化，改动 agent-facing 文档时运行：

```powershell
dotnet test ProjectContextCompiler.slnx --filter FullyQualifiedName~DocumentationFreshnessTests
```

当前覆盖：

- `README.md` 能链接到 `docs/README.md`。
- `AGENTS.md` 中列出的关键文档存在。
- `docs/README.md` 中列出的链接存在。
- `docs/` 内 Markdown 相对链接可解析。
- `docs/superpowers/` 不作为长期事实来源。
- 新增长期规则时同步更新 `docs/decisions.md` 或对应设计文档。
