# HomeMesh Agent Charter Entry

本仓库的统一工程规范以以下文件为准：

- `.github/instructions/homemesh-charter.instructions.md`

## 适用对象

- GitHub Copilot
- Codex 系代理
- CC（Claude Code）及其他可读取仓库约束文件的代理

## 使用要求（轻量）

- 修改 `src`、`tests`、`docs` 时，默认遵循宪章。
- 遇到冲突时，以宪章中的“原则冲突优先级”为决策顺序。
- 不在不同代理入口维护相互冲突的规则；如需变更，请先更新宪章，再同步入口文件。
