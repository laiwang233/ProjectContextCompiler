# PCC Flow v1.1 Stitch 设计稿 Bundle

本目录是 `PCC Flow v1.1` 的本地设计稿归档。后续 agent 即使无法访问 Stitch，也可以通过这里的逐页截图和 manifest 理解当前原型。

## 内容（Contents）

- [manifest.json](manifest.json)：Stitch 项目、screen id、路由、截图路径和关键业务对象映射。
- [screens/contact-sheet.png](screens/contact-sheet.png)：8 页总览图。
- `screens/01-projects.png` 到 `screens/08-export.png`：逐页设计稿截图。

## 使用方式（Usage）

1. 先读 [`../pcc-flow-v1.1-stitch.md`](../pcc-flow-v1.1-stitch.md)，了解页面范围、Gate 和 UI 口径。
2. 再打开 [screens/contact-sheet.png](screens/contact-sheet.png) 建立整体视觉印象。
3. 实现或评审某个页面时，打开对应逐页截图。
4. 需要程序化读取页面映射时，读取 [manifest.json](manifest.json)。

## 截图验收（Screenshot Verification）

- 2026-06-09 已检查本目录下 8 张逐页截图和 contact sheet。
- `screens/08-export.png` 是当前保留的 `PCC Flow v1.1 - 导出 (修正布局)` 版本，右侧 `Export Readiness`、Included/Excluded、校验清单和诊断面板存在。
- `screens/06-review.png` 的审核动作区仍有轻微截断风险；后续实现不能照抄该按钮宽度，必须完整展示五个动作名称。
- 当前 bundle 可作为阶段原型上下文使用，但不表示所有视觉细节都已达到实现验收标准。

## 边界（Boundaries）

- 这些图片是设计稿资产，不是前端源码。
- 不要把 Stitch 生成 HTML 直接复制进 `frontend/`。
- 还原界面时仍应使用 Ant Design 工作流后台模式，并以 `../architecture.md`、`../decisions.md` 和 `../../MVP_Spec.md` 为最终约束。
