# 数据库草案

## 1. 数据库设计目标

第一阶段数据库承担两类数据：

1. HomeMesh 自己的业务数据
2. ZeroTier Provider 返回数据的本地映射和扩展字段

原则：

- HomeMesh 内部 ID 与 Provider ID 分离
- Provider 原生字段不直接作为业务主键
- HomeMesh 扩展字段以本地数据库为准
- Provider 状态可以同步刷新
- 第一阶段使用 SQLite，后续支持 PostgreSQL

## 2. ID 约定

建议使用带前缀的字符串 ID：

| 实体 | 示例 |
|---|---|
| Home | `home_01HX...` |
| Network | `hmnet_01HX...` |
| Device | `dev_01HX...` |
| Member | `member_01HX...` |
| Route | `route_01HX...` |
| IP Pool | `ippool_01HX...` |
| Enrollment Token | `enroll_01HX...` |
| Audit Log | `audit_01HX...` |

## 3. 表结构

## 3.1 Users

管理员用户。

| 字段 | 类型 | 说明 |
|---|---|---|
| Id | string | 主键 |
| Username | string | 用户名，唯一 |
| PasswordHash | string | 密码哈希 |
| Role | string | Admin |
| CreatedAt | datetime | 创建时间 |
| UpdatedAt | datetime | 更新时间 |
| LastLoginAt | datetime? | 最后登录时间 |
| Disabled | bool | 是否禁用 |

## 3.2 Homes

家庭空间。

| 字段 | 类型 | 说明 |
|---|---|---|
| Id | string | 主键 |
| Name | string | 家庭名称 |
| OwnerUserId | string | 所属管理员 |
| CreatedAt | datetime | 创建时间 |
| UpdatedAt | datetime | 更新时间 |

第一阶段默认 Seed 一个 Home。

## 3.3 Networks

HomeMesh 内部虚拟网络。

| 字段 | 类型 | 说明 |
|---|---|---|
| Id | string | 主键 |
| HomeId | string | 所属 Home |
| Name | string | 网络名称 |
| Cidr | string | 主 CIDR |
| Private | bool | 是否私有网络 |
| V4AssignMode | bool | 是否启用 IPv4 自动分配 |
| V6AssignModeJson | string? | IPv6 分配配置 JSON |
| Status | string | Healthy / Warning / Offline |
| CreatedAt | datetime | 创建时间 |
| UpdatedAt | datetime | 更新时间 |

## 3.4 NetworkProviderBindings

HomeMesh Network 与底层协议网络的绑定关系。

| 字段 | 类型 | 说明 |
|---|---|---|
| Id | string | 主键 |
| NetworkId | string | HomeMesh Network ID |
| Provider | string | ZeroTier / WireGuard / etc |
| ProviderNetworkId | string | 底层协议网络 ID |
| ProviderConfigJson | string? | 协议专属配置 |
| IsPrimary | bool | 是否主协议 |
| CreatedAt | datetime | 创建时间 |
| UpdatedAt | datetime | 更新时间 |

唯一索引：

- NetworkId + Provider
- Provider + ProviderNetworkId

## 3.5 Devices

真实设备。

| 字段 | 类型 | 说明 |
|---|---|---|
| Id | string | 主键 |
| HomeId | string | 所属 Home |
| Name | string | 设备名称 |
| Platform | string | Windows / Linux / Android / iOS / NAS / Unknown |
| Fingerprint | string? | 设备指纹 |
| PublicKey | string? | 设备公钥 |
| TrustLevel | string | Trusted / Normal / Pending |
| TagsJson | string? | 标签 JSON |
| LastSeenAt | datetime? | 最后在线时间 |
| CreatedAt | datetime | 创建时间 |
| UpdatedAt | datetime | 更新时间 |

## 3.6 NetworkMembers

设备在网络中的成员身份。

| 字段 | 类型 | 说明 |
|---|---|---|
| Id | string | 主键 |
| NetworkId | string | HomeMesh Network ID |
| DeviceId | string? | 绑定真实设备，可空 |
| Provider | string | ZeroTier |
| ProviderMemberId | string | 底层协议成员 ID |
| Name | string? | 成员名称 |
| Role | string | Personal / Gateway / Service / Mobile / Pending / Unknown |
| Authorized | bool | 是否授权 |
| ActiveBridge | bool | 是否桥接 |
| Online | bool | 是否在线 |
| IpAssignmentsJson | string? | IP 列表 JSON |
| TagsJson | string? | 标签 JSON |
| ProviderRawJson | string? | Provider 原始数据快照 |
| LastSeenAt | datetime? | 最后在线时间 |
| CreatedAt | datetime | 创建时间 |
| UpdatedAt | datetime | 更新时间 |

唯一索引：

- NetworkId + Provider + ProviderMemberId

## 3.7 Routes

网络路由。

| 字段 | 类型 | 说明 |
|---|---|---|
| Id | string | 主键 |
| NetworkId | string | 所属 Network |
| Type | string | VirtualSubnet / LanSubnet / ExitRoute / IPv6 |
| Target | string | CIDR，例如 192.168.1.0/24 |
| Via | string? | 网关 IP |
| Enabled | bool | 是否启用 |
| ProviderManaged | bool | 是否同步到 Provider |
| CreatedAt | datetime | 创建时间 |
| UpdatedAt | datetime | 更新时间 |

## 3.8 IpPools

IP 分配池。

| 字段 | 类型 | 说明 |
|---|---|---|
| Id | string | 主键 |
| NetworkId | string | 所属 Network |
| IpRangeStart | string | 起始 IP |
| IpRangeEnd | string | 结束 IP |
| ProviderManaged | bool | 是否同步到 Provider |
| CreatedAt | datetime | 创建时间 |
| UpdatedAt | datetime | 更新时间 |

## 3.9 DnsConfigs

DNS 配置。

| 字段 | 类型 | 说明 |
|---|---|---|
| Id | string | 主键 |
| NetworkId | string | 所属 Network，唯一 |
| Domain | string? | 搜索域 |
| ServersJson | string | DNS servers JSON |
| SplitDnsJson | string? | Split DNS 预留 |
| ProviderManaged | bool | 是否同步到 Provider |
| CreatedAt | datetime | 创建时间 |
| UpdatedAt | datetime | 更新时间 |

## 3.10 Gateways

网关设备。

| 字段 | 类型 | 说明 |
|---|---|---|
| Id | string | 主键 |
| NetworkId | string | 所属 Network |
| DeviceId | string? | 真实设备 ID |
| MemberId | string | NetworkMember ID |
| Name | string | 网关名称 |
| LanSubnetsJson | string? | LAN 子网 JSON |
| SubnetRouterEnabled | bool | 是否启用子网路由 |
| ExitNodeEnabled | bool | 是否启用出口节点 |
| HealthStatus | string | Healthy / Warning / Offline |
| LastHeartbeatAt | datetime? | 最后心跳 |
| CreatedAt | datetime | 创建时间 |
| UpdatedAt | datetime | 更新时间 |

## 3.11 EnrollmentTokens

设备邀请令牌。

| 字段 | 类型 | 说明 |
|---|---|---|
| Id | string | 主键 |
| HomeId | string | 所属 Home |
| NetworkId | string | 目标网络 |
| Code | string | 邀请码，唯一 |
| DefaultTagsJson | string? | 默认标签 |
| AutoAuthorize | bool | 是否自动授权 |
| MaxUses | int? | 最大使用次数 |
| UsedCount | int | 已使用次数 |
| ExpiresAt | datetime | 过期时间 |
| RevokedAt | datetime? | 撤销时间 |
| CreatedByUserId | string | 创建人 |
| CreatedAt | datetime | 创建时间 |

## 3.12 AclPolicies

ACL 策略。

第一阶段仅建模和 UI 预留，不做强执行。

| 字段 | 类型 | 说明 |
|---|---|---|
| Id | string | 主键 |
| HomeId | string | 所属 Home |
| NetworkId | string? | 所属 Network |
| Name | string | 策略名称 |
| Description | string? | 说明 |
| Enabled | bool | 是否启用 |
| Priority | int | 优先级 |
| RulesJson | string | 规则 JSON |
| CreatedAt | datetime | 创建时间 |
| UpdatedAt | datetime | 更新时间 |

## 3.13 AuditLogs

审计日志。

| 字段 | 类型 | 说明 |
|---|---|---|
| Id | string | 主键 |
| HomeId | string? | 所属 Home |
| UserId | string? | 操作用户 |
| Type | string | 日志类型 |
| Actor | string | 操作者名称 |
| TargetType | string? | 目标类型 |
| TargetId | string? | 目标 ID |
| Message | string | 日志消息 |
| MetadataJson | string? | 扩展信息 |
| IpAddress | string? | 请求 IP |
| UserAgent | string? | UA |
| CreatedAt | datetime | 创建时间 |

## 3.14 ProviderSyncStates

Provider 同步状态。

| 字段 | 类型 | 说明 |
|---|---|---|
| Id | string | 主键 |
| Provider | string | Provider 名称 |
| ResourceType | string | Network / Member / Route / etc |
| ResourceId | string? | 资源 ID |
| LastSyncAt | datetime? | 最后同步时间 |
| LastError | string? | 最后错误 |
| Status | string | Healthy / Error |

## 4. 第一阶段索引建议

- Users.Username unique
- NetworkProviderBindings.Provider + ProviderNetworkId unique
- NetworkMembers.NetworkId + Provider + ProviderMemberId unique
- EnrollmentTokens.Code unique
- AuditLogs.CreatedAt index
- Devices.HomeId + Fingerprint index
- Routes.NetworkId index
- IpPools.NetworkId index

## 5. 数据同步原则

### 5.1 Provider 原生字段

例如：

- ZeroTier Network ID
- ZeroTier Member ID
- ZeroTier authorized
- ZeroTier routes
- ZeroTier ipAssignmentPools

这些字段应从 Provider 同步。

### 5.2 HomeMesh 扩展字段

例如：

- Device name
- Tags
- Role
- Gateway 配置
- Enrollment Token
- AuditLog

这些字段以 HomeMesh 数据库为准。

## 6. 未来迁移策略

第一阶段 SQLite。

第二阶段增加 PostgreSQL 支持。

建议所有 migration 使用 EF Core 管理，不手写 SQL 作为主流程。
