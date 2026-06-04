# 技术债跟踪

本文记录执行中发现但不属于当前里程碑的后续事项。每条事项应包含来源、影响、建议处理方式和验证入口。

## 当前事项

- 2026-06-04：新增架构依赖方向测试。
  - 来源：当前文档约束了四层依赖方向，但测试尚未机械化。
  - 影响：后续改动可能引入 `Application -> Infrastructure` 或 `Domain -> EF Core` 等反向依赖。
  - 建议：引入轻量架构测试，或用 MSBuild/project reference 解析测试校验依赖方向。
  - 验证：`dotnet test ProjectContextCompiler.slnx`。
