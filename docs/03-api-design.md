# API 草案

## 1. API 设计目标

HomeMesh API 同时服务：

- Web Admin 管理后台
- 未来 Windows 客户端
- 未来 Android 客户端
- 未来 Linux Gateway Agent
- 未来 iOS 客户端

第一阶段 API 重点服务 Web Admin，但命名与模型应面向长期客户端接入。

## 2. API 基础约定

### 2.1 Base Path

```text
/api
```

### 2.2 返回格式

建议统一包装：

```json
{
  "success": true,
  "data": {},
  "error": null,
  "traceId": "..."
}
```

错误格式：

```json
{
  "success": false,
  "data": null,
  "error": {
    "code": "NETWORK_NOT_FOUND",
    "message": "Network not found"
  },
  "traceId": "..."
}
```

### 2.3 认证

第一阶段：

- Web Admin 使用 Cookie / Session 或 JWT 均可
- API 默认需要认证
- `/api/setup/*` 和 `/api/auth/login` 例外

## 3. Setup API

```http
GET  /api/setup/status
POST /api/setup/admin
```

### GET /api/setup/status

用于判断是否已经初始化管理员。

返回：

```json
{
  "initialized": false
}
```

### POST /api/setup/admin

初始化管理员账号。

请求：

```json
{
  "username": "admin",
  "password": "change-me"
}
```

## 4. Auth API

```http
POST /api/auth/login
POST /api/auth/logout
GET  /api/auth/me
```

## 5. Dashboard API

```http
GET /api/dashboard/summary
GET /api/dashboard/recent-events
```

### GET /api/dashboard/summary

返回：

```json
{
  "networkCount": 3,
  "deviceCount": 15,
  "onlineDeviceCount": 12,
  "gatewayCount": 2,
  "policyCount": 18,
  "providerStatus": "Healthy"
}
```

## 6. Provider API

```http
GET  /api/providers
GET  /api/providers/{providerName}/status
POST /api/providers/{providerName}/test
GET  /api/providers/zerotier/config
PUT  /api/providers/zerotier/config
```

### Provider 对象

```json
{
  "name": "ZeroTier",
  "displayName": "ZeroTier",
  "enabled": true,
  "status": "Healthy",
  "capabilities": ["NetworkManagement", "MemberManagement", "Routes", "Dns"]
}
```

### ZeroTier 配置

```json
{
  "apiBaseUrl": "http://127.0.0.1:9993",
  "authTokenPath": "/var/lib/zerotier-one/authtoken.secret",
  "enabled": true
}
```

## 7. Network API

```http
GET    /api/networks
POST   /api/networks
GET    /api/networks/{networkId}
PATCH  /api/networks/{networkId}
DELETE /api/networks/{networkId}
POST   /api/networks/{networkId}/easy-setup
POST   /api/networks/{networkId}/sync
```

### Network 对象

```json
{
  "id": "hmnet_01HX...",
  "homeId": "home_01HX...",
  "name": "主家庭网络",
  "cidr": "10.10.0.0/24",
  "provider": "ZeroTier",
  "providerNetworkId": "8056c2e21c000001",
  "memberCount": 8,
  "onlineMemberCount": 6,
  "gatewayCount": 1,
  "status": "Healthy"
}
```

### POST /api/networks

```json
{
  "homeId": "home_01HX...",
  "name": "主家庭网络",
  "provider": "ZeroTier",
  "cidr": "10.10.0.0/24"
}
```

### PATCH /api/networks/{networkId}

```json
{
  "name": "主家庭网络",
  "private": true,
  "v4AssignMode": true,
  "v6AssignMode": {
    "zt": false,
    "rfc4193": false,
    "sixPlane": false
  }
}
```

### POST /api/networks/{networkId}/easy-setup

```json
{
  "cidr": "10.10.0.0/24",
  "poolStart": "10.10.0.10",
  "poolEnd": "10.10.0.200",
  "dnsDomain": "home.arpa",
  "dnsServers": ["10.10.0.1"]
}
```

## 8. Member API

```http
GET    /api/networks/{networkId}/members
GET    /api/networks/{networkId}/members/{memberId}
PATCH  /api/networks/{networkId}/members/{memberId}
DELETE /api/networks/{networkId}/members/{memberId}
POST   /api/networks/{networkId}/members/{memberId}/authorize
POST   /api/networks/{networkId}/members/{memberId}/deauthorize
POST   /api/networks/{networkId}/members/{memberId}/ips
DELETE /api/networks/{networkId}/members/{memberId}/ips/{ip}
```

### Member 对象

```json
{
  "id": "member_01HX...",
  "networkId": "hmnet_01HX...",
  "providerMemberId": "a8f12c3300",
  "name": "家里 Linux 网关",
  "role": "Gateway",
  "platform": "Linux",
  "authorized": true,
  "activeBridge": true,
  "online": true,
  "ipAssignments": ["10.10.0.1"],
  "tags": ["gateway", "home-lan"],
  "lastSeenAt": "2026-05-17T10:00:00Z"
}
```

### PATCH Member

```json
{
  "name": "家里 Linux 网关",
  "role": "Gateway",
  "activeBridge": true,
  "tags": ["gateway", "home-lan"]
}
```

### POST Member IP

```json
{
  "ipAddress": "10.10.0.1"
}
```

## 9. Route API

```http
GET    /api/networks/{networkId}/routes
POST   /api/networks/{networkId}/routes
DELETE /api/networks/{networkId}/routes/{routeId}
```

### Route 对象

```json
{
  "id": "route_01HX...",
  "type": "LanSubnet",
  "target": "192.168.1.0/24",
  "via": "10.10.0.1",
  "enabled": true
}
```

### POST Route

```json
{
  "type": "LanSubnet",
  "target": "192.168.1.0/24",
  "via": "10.10.0.1"
}
```

## 10. IP Pool API

```http
GET    /api/networks/{networkId}/ip-pools
POST   /api/networks/{networkId}/ip-pools
DELETE /api/networks/{networkId}/ip-pools/{poolId}
```

### IP Pool 对象

```json
{
  "id": "ippool_01HX...",
  "ipRangeStart": "10.10.0.10",
  "ipRangeEnd": "10.10.0.200"
}
```

## 11. DNS API

```http
GET /api/networks/{networkId}/dns
PUT /api/networks/{networkId}/dns
```

### DNS 对象

```json
{
  "domain": "home.arpa",
  "servers": ["10.10.0.1", "1.1.1.1"]
}
```

## 12. Enrollment API

```http
POST   /api/enrollment/tokens
GET    /api/enrollment/tokens
DELETE /api/enrollment/tokens/{tokenId}
POST   /api/devices/enroll
POST   /api/devices/heartbeat
GET    /api/devices/me/config
```

### POST /api/enrollment/tokens

```json
{
  "networkId": "hmnet_01HX...",
  "expiresInMinutes": 10080,
  "defaultTags": ["pending"],
  "autoAuthorize": false
}
```

返回：

```json
{
  "id": "enroll_01HX...",
  "code": "HMM-728A-K3Q2-9F6B",
  "networkId": "hmnet_01HX...",
  "expiresAt": "2026-05-24T10:00:00Z"
}
```

### POST /api/devices/enroll

```json
{
  "code": "HMM-728A-K3Q2-9F6B",
  "deviceName": "Windows 工作站",
  "platform": "Windows",
  "publicKey": "...",
  "fingerprint": "..."
}
```

## 13. Gateway API

```http
GET   /api/gateways
GET   /api/gateways/{gatewayId}
PATCH /api/gateways/{gatewayId}
POST  /api/gateways/{gatewayId}/enable-subnet-router
POST  /api/gateways/{gatewayId}/enable-exit-node
POST  /api/gateways/{gatewayId}/disable-exit-node
```

第一阶段 Gateway API 可先作为模型预留。

## 14. ACL API

```http
GET  /api/policies
POST /api/policies
GET  /api/policies/{policyId}
PUT  /api/policies/{policyId}
DELETE /api/policies/{policyId}
```

第一阶段 ACL 不做强执行，只保存模型和展示。

## 15. Audit API

```http
GET /api/audit-logs
```

查询参数：

```text
?type=MemberAuthorized&networkId=hmnet_xxx&from=2026-05-01&to=2026-05-17
```

### AuditLog 对象

```json
{
  "id": "audit_01HX...",
  "type": "MemberAuthorized",
  "actor": "admin",
  "targetType": "Member",
  "targetId": "member_01HX...",
  "message": "管理员授权了 Android 手机",
  "createdAt": "2026-05-17T10:00:00Z"
}
```
