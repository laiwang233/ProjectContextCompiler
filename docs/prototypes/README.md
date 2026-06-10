# 原型索引

本目录保存 Project Context Compiler 的本地原型资料。这里的内容用于产品走查、UI 还原和 agent 上下文，不是可运行前端实现，也不是 API 或领域模型的最终事实来源。

## 当前原型

- [PCC Flow v1.1 Stitch 全流程原型](pcc-flow-v1.1-stitch.md)
- [PCC Flow v1.1 Stitch 设计稿 Bundle](pcc-flow-v1.1-stitch/README.md)
- [PCC Flow v1.1 contact sheet](pcc-flow-v1.1-stitch/screens/contact-sheet.png)

## 历史原型



## 使用规则

- 后续 agent 涉及前端 UI、工作流页面或界面文案时，先读 `../architecture.md` 和 `../decisions.md`，再读本目录。
- Stitch 原型用于理解信息架构、页面密度、Gate 文案和关键对象状态；实现仍以 `MVP_Spec.md`、`../architecture.md` 和后端测试为准。
- 不要把 Stitch 生成的 HTML 直接复制进 `frontend/`；需要实现时用 Ant Design 工作流后台模式重新落地。
