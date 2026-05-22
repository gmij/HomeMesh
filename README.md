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

当前仓库处于 `v0.1 Server MVP` 阶段。

已经具备：

1. HomeMesh Controller 服务端
2. ZeroTier Provider 基础接入
3. Demo Provider 演示模式
4. 内置 Web Admin 管理后台
5. SQLite 数据库模型
6. 管理员初始化与登录
7. 网络、路由、IP 池、DNS、成员管理
8. 成员同步、配置同步、审计日志
9. Docker / GitHub Actions CI

## MVP 快速启动

### 本地运行

需要安装 .NET 10 SDK。

```bash
dotnet restore HomeMesh.sln
dotnet run --project src/HomeMesh.WebApi/HomeMesh.WebApi.csproj
```

打开：

```text
http://localhost:5000/
```

如果使用 Docker 暴露端口，则通常是：

```text
http://localhost:8080/
```

首次打开管理台时，系统会提示创建第一个管理员账号。创建后会自动登录。

### Docker 运行

```bash
docker compose -f deploy/docker/docker-compose.yml up -d --build
```

访问：

```text
http://localhost:8080/
```

## Demo Provider 演示模式

没有 ZeroTier 环境时，也可以完整体验 MVP 流程。

1. 打开 `/`
2. 创建管理员账号并登录
3. 创建网络时选择 `Demo 演示`
4. 使用 Easy Setup 设置 CIDR、IP 池和 DNS
5. 点击“同步配置”
6. 点击“同步成员”
7. 在成员列表中授权或取消授权演示设备
8. 查看 Dashboard、同步状态和审计日志

Demo Provider 是内存态，仅用于演示与本地试跑。服务重启后，Demo Provider 内部的虚拟 Provider 状态会丢失，但 HomeMesh 本地 SQLite 中的网络记录仍会保留。

## ZeroTier Provider 模式

### 连接 ZeroTier One Controller

HomeMesh 和 ztncui 一样，默认连接同机的 ZeroTier One Local API，并读取：

```text
/var/lib/zerotier-one/authtoken.secret
```

也就是 ZeroTier One 进程仍负责底层 controller API，HomeMesh 负责替代 ztncui 的管理界面和 HomeMesh 自己的控制面编排。

如果 HomeMesh 跑在 Docker 中，后续可以把 ZeroTier One 集成到同一个部署单元里；当前需要让容器能访问宿主机的 Local API，并读取宿主机的 token 文件。

在 `deploy/docker/docker-compose.yml` 中启用类似配置：

```yaml
volumes:
  - homemesh-data:/app/data
  - /var/lib/zerotier-one/authtoken.secret:/var/lib/zerotier-one/authtoken.secret:ro
```

默认访问：

```text
http://127.0.0.1:9993
```

如果 ZeroTier One 不在同一个网络命名空间里，需要调整 `Providers:ZeroTier:ApiBaseUrl`。

## MVP 操作流程

1. 打开 `/`
2. 创建管理员账号
3. 登录管理台
4. 创建一个网络，演示环境选择 `Demo 演示`，真实环境选择 `ZeroTier`
5. 使用 Easy Setup 设置 CIDR、IP 池和 DNS
6. 点击“同步配置”推送到 Provider
7. 设备加入真实 ZeroTier 网络后，或 Demo 网络创建后，点击“同步成员”
8. 在成员列表中授权或取消授权设备
9. 查看同步状态和审计日志

## 常用 API

```text
GET  /health
GET  /api/auth/status
POST /api/auth/login
POST /api/auth/logout
GET  /api/dashboard/summary
GET  /api/networks
POST /api/networks
POST /api/networks/{networkId}/easy-setup
POST /api/networks/{networkId}/config/sync
POST /api/networks/{networkId}/members/sync
GET  /api/audit-logs
```

业务 API 需要登录后的 `hm_session` Cookie。

## 文档

- [第一阶段需求分析](docs/01-phase-1-requirements.md)
- [总体架构设计](docs/02-architecture.md)
- [API 草案](docs/03-api-design.md)
- [数据库草案](docs/04-database-design.md)
- [任务拆解](docs/05-task-breakdown.md)
- [开发指南](docs/06-development-guide.md)
- [v0.1 MVP Checklist](docs/07-mvp-checklist.md)

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
