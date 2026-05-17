# 总体架构设计

## 1. 架构目标

HomeMesh Controller 的架构目标是：

> 以 Home SD-WAN 控制面为核心，第一阶段接入 ZeroTier，未来可扩展 WireGuard、Headscale、自研 QUIC 等协议。

因此，架构必须避免直接绑定 ZeroTier 概念。

## 2. 总体架构

```text
┌──────────────────────────────┐
│        Web Admin UI           │
│  Dashboard / Networks / etc.  │
└───────────────┬──────────────┘
                │ REST / SignalR
┌───────────────▼──────────────┐
│        HomeMesh Web API       │
│  Auth / Network / Member API  │
└───────────────┬──────────────┘
                │
┌───────────────▼──────────────┐
│      Application Services     │
│ NetworkService / DeviceService│
└───────────────┬──────────────┘
                │
┌───────────────▼──────────────┐
│          Domain Model         │
│ Home / Network / Device / etc │
└───────────────┬──────────────┘
                │
┌───────────────▼──────────────┐
│       Infrastructure Layer    │
│ EF Core / Audit / Config      │
└───────────────┬──────────────┘
                │
┌───────────────▼──────────────┐
│       Provider Abstraction    │
│ ISdwanControllerProvider      │
└───────┬──────────────┬───────┘
        │              │
┌───────▼───────┐ ┌────▼────────┐
│ ZeroTier      │ │ WireGuard   │
│ Provider      │ │ Reserved    │
└───────────────┘ └─────────────┘
```

## 3. 推荐项目结构

```text
HomeMesh.sln

src/
  HomeMesh.Abstractions/
  HomeMesh.Domain/
  HomeMesh.Application/
  HomeMesh.Infrastructure/
  HomeMesh.Protocol.ZeroTier/
  HomeMesh.WebApi/
  HomeMesh.WebAdmin/
  HomeMesh.Worker/

tests/
  HomeMesh.Tests/

deploy/
  docker/
  systemd/
```

## 4. 分层说明

### 4.1 HomeMesh.Abstractions

放置跨模块共享的接口和 DTO。

包括：

- ISdwanControllerProvider
- Provider capability
- VirtualNetworkInfo
- VirtualMemberInfo
- VirtualRoute
- VirtualDnsConfig

### 4.2 HomeMesh.Domain

放置领域模型。

包括：

- Home
- Network
- NetworkProviderBinding
- Device
- NetworkMember
- Gateway
- Route
- IpPool
- DnsConfig
- EnrollmentToken
- AuditLog

### 4.3 HomeMesh.Application

放置业务服务。

包括：

- NetworkService
- MemberService
- DeviceService
- EnrollmentService
- GatewayService
- ProviderService
- AuditService

### 4.4 HomeMesh.Infrastructure

放置技术实现。

包括：

- EF Core DbContext
- Repository
- Migration
- Config Provider
- Audit Writer
- Password Hasher
- Token Generator

### 4.5 HomeMesh.Protocol.ZeroTier

封装 ZeroTier Local API。

包括：

- ZeroTierLocalApiClient
- ZeroTierControllerProvider
- ZeroTierModels
- ZeroTierModelMapper

### 4.6 HomeMesh.WebApi

暴露 REST API。

包括：

- Auth API
- Provider API
- Network API
- Member API
- Route API
- DNS API
- Enrollment API
- Audit API

### 4.7 HomeMesh.WebAdmin

现代化 Web 管理后台。

建议第一阶段使用 React + Ant Design / Tailwind 风格实现。

### 4.8 HomeMesh.Worker

后台同步任务。

包括：

- Provider 状态同步
- 成员在线状态同步
- 审计日志清理
- 邀请 Token 过期处理

## 5. Provider 抽象

核心思想：

> HomeMesh 的 Network 不等于 ZeroTier Network。

ProviderBinding 用于绑定 HomeMesh 网络和底层协议网络。

### 5.1 核心接口

```csharp
public interface ISdwanControllerProvider
{
    string ProviderName { get; }

    Task<ProviderHealthStatus> GetStatusAsync(CancellationToken ct);

    Task<IReadOnlyList<VirtualNetworkInfo>> ListNetworksAsync(CancellationToken ct);

    Task<VirtualNetworkInfo> CreateNetworkAsync(
        CreateVirtualNetworkRequest request,
        CancellationToken ct);

    Task<VirtualNetworkInfo> GetNetworkAsync(
        string providerNetworkId,
        CancellationToken ct);

    Task<VirtualNetworkInfo> UpdateNetworkAsync(
        string providerNetworkId,
        UpdateVirtualNetworkRequest request,
        CancellationToken ct);

    Task DeleteNetworkAsync(
        string providerNetworkId,
        CancellationToken ct);

    Task<IReadOnlyList<VirtualMemberInfo>> ListMembersAsync(
        string providerNetworkId,
        CancellationToken ct);

    Task<VirtualMemberInfo> UpdateMemberAsync(
        string providerNetworkId,
        string providerMemberId,
        UpdateVirtualMemberRequest request,
        CancellationToken ct);
}
```

## 6. 状态同步策略

第一阶段采用主动同步 + 手动刷新结合。

- 页面打开时查询 Provider 实时状态
- 后台 Worker 定期同步网络和成员状态
- 关键操作后立即同步对应资源
- HomeMesh 扩展字段以本地数据库为准
- Provider 原生字段以 Provider 返回值为准

## 7. 安全架构

第一阶段安全要求：

- 管理后台必须登录
- 管理员密码哈希保存
- Provider Token 不在前端明文展示
- 关键操作写入审计日志
- API 默认需要认证
- Docker 部署默认只监听内网或本机
- 后续支持 HTTPS / Reverse Proxy

## 8. 部署架构

### 8.1 单机部署

```text
Linux Host
├─ zerotier-one
├─ homemesh-controller
└─ sqlite database
```

### 8.2 Docker 部署

```text
Docker Compose
├─ homemesh-controller
├─ volume: data
└─ bind mount: /var/lib/zerotier-one/authtoken.secret:ro
```

## 9. 第一阶段架构原则

- 不复制 ztncui GPLv3 代码
- 不把业务模型命名为 ZeroTierXXX
- Provider 只负责协议适配
- Application Service 负责业务编排
- Web Admin 只调用 HomeMesh API
- 所有关键操作可审计
- 第一版简单部署优先
