# HomeMesh

HomeMesh 是一套面向家庭与小型办公场景的 Home SD-WAN 控制平台。

第一阶段目标是构建一个基于 .NET 10 的服务端控制面，用于替代 ztncui，并为未来多协议扩展预留架构基础。

## 第一阶段目标

- 替代 ztncui 的 ZeroTier Controller 管理能力
- 建立 Home / Network / Device / Member / Gateway 等 Home SD-WAN 领域模型
- 通过 Provider 抽象支持未来 ZeroTier、WireGuard、Headscale、自研 QUIC 等协议
- 提供现代化 Web 管理后台
- 提供标准 REST API，供未来 Windows / Android / Linux / iOS 客户端接入
- 支持 Docker / Linux 服务端部署

## 当前阶段

当前仓库处于 `v0.1 Server MVP` 规划阶段。

优先建设：

1. HomeMesh Controller 服务端
2. ZeroTier Provider
3. Web Admin 管理后台
4. SQLite 数据库模型
5. 设备接入 Enrollment
6. 审计日志

## 文档

- [第一阶段需求分析](docs/01-phase-1-requirements.md)
- [总体架构设计](docs/02-architecture.md)
- [API 草案](docs/03-api-design.md)
- [数据库草案](docs/04-database-design.md)
- [任务拆解](docs/05-task-breakdown.md)

## 产品方向

HomeMesh 不是单纯的 VPN 工具，也不是简单的 ZeroTier UI。它的目标是成为家庭网络的统一控制面：

- 统一连接
- 统一设备管理
- 统一路由策略
- 统一安全访问
- 统一多协议扩展

## 许可证

待定。

第一阶段建议先保持商业友好方向，避免直接引入 GPLv3 代码。ztncui 仅作为功能参考，不直接复制代码。
