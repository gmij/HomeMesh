# 第一阶段任务拆解

## 1. 阶段目标

第一阶段目标：

> 做出一个可以替代 ztncui 的 .NET 10 HomeMesh Controller，并为未来多协议 Home SD-WAN 架构打基础。

## 2. 优先级说明

| 优先级 | 说明 |
|---|---|
| P0 | 第一阶段必须完成，否则不能替代 ztncui |
| P1 | 第一阶段尽量完成，用于体现 HomeMesh 差异化 |
| P2 | 预留或展示即可，不作为第一阶段交付阻塞项 |

## 3. Epic 1：项目初始化

目标：搭建 .NET 10 服务端基础工程。

| 编号 | 任务 | 优先级 | 验收标准 |
|---|---|---:|---|
| T1.1 | 创建 .NET 10 Web API 工程 | P0 | 本地可启动 |
| T1.2 | 创建 Domain / Application / Infrastructure / WebApi 分层 | P0 | 项目结构清晰 |
| T1.3 | 接入 SQLite + EF Core | P0 | 可创建数据库 |
| T1.4 | 接入 Serilog | P0 | 控制台和文件日志可用 |
| T1.5 | 接入 OpenAPI / Swagger | P0 | `/swagger` 可访问 |
| T1.6 | 添加 Dockerfile | P0 | 可构建镜像 |
| T1.7 | 添加 docker-compose 示例 | P0 | 可一键启动 |
| T1.8 | 初始化管理员账号 | P0 | 首次启动可设置 admin |
| T1.9 | 添加 `/health` | P0 | 返回服务健康状态 |

## 4. Epic 2：领域模型与数据库

目标：建立 HomeMesh 自己的数据模型。

| 编号 | 任务 | 优先级 | 验收标准 |
|---|---|---:|---|
| T2.1 | Home 实体 | P0 | 可保存家庭空间 |
| T2.2 | Network 实体 | P0 | 可保存 HomeMesh 网络 |
| T2.3 | ProviderBinding 实体 | P0 | 可绑定 ZeroTier Network ID |
| T2.4 | Device 实体 | P0 | 可保存真实设备 |
| T2.5 | NetworkMember 实体 | P0 | 可保存网络成员 |
| T2.6 | Route 实体 | P0 | 可保存路由 |
| T2.7 | IpPool 实体 | P0 | 可保存 IP 池 |
| T2.8 | DnsConfig 实体 | P0 | 可保存 DNS |
| T2.9 | EnrollmentToken 实体 | P0 | 可生成邀请码 |
| T2.10 | AuditLog 实体 | P0 | 可写入审计日志 |
| T2.11 | Gateway 实体 | P1 | 可标记网关节点 |
| T2.12 | AclPolicy 实体 | P1 | 可保存策略草案 |
| T2.13 | EF Migration | P0 | 数据库可迁移 |
| T2.14 | Seed 默认 Home | P0 | 初始环境可用 |

## 5. Epic 3：协议 Provider 抽象

目标：让 ZeroTier 成为第一种 Provider，而不是系统唯一协议。

| 编号 | 任务 | 优先级 | 验收标准 |
|---|---|---:|---|
| T3.1 | 定义 ISdwanControllerProvider | P0 | ZeroTier 可实现该接口 |
| T3.2 | 定义 VirtualNetworkInfo | P0 | 网络信息协议无关 |
| T3.3 | 定义 VirtualMemberInfo | P0 | 成员信息协议无关 |
| T3.4 | 定义 VirtualRoute | P0 | 路由信息协议无关 |
| T3.5 | 定义 VirtualDnsConfig | P0 | DNS 信息协议无关 |
| T3.6 | 定义 ProviderHealthStatus | P0 | 可展示 Provider 健康状态 |
| T3.7 | Provider 注册机制 | P0 | 可按名称获取 Provider |
| T3.8 | Provider Capability 声明 | P1 | UI 可展示能力差异 |
| T3.9 | WireGuard Provider 空实现 | P2 | 页面可显示预留状态 |

## 6. Epic 4：ZeroTier Provider

目标：对接本机 ZeroTier One API。

| 编号 | 任务 | 优先级 | 验收标准 |
|---|---|---:|---|
| T4.1 | 读取 ZeroTier 配置 | P0 | appsettings 可配置 |
| T4.2 | 读取 authtoken.secret | P0 | 可从文件读取 token |
| T4.3 | 实现 ZeroTierLocalApiClient | P0 | 可调用本机 API |
| T4.4 | 获取 ZeroTier status | P0 | Provider 状态可展示 |
| T4.5 | 获取 networks | P0 | 网络列表可展示 |
| T4.6 | 创建 network | P0 | 可创建 ZeroTier 网络 |
| T4.7 | 删除 network | P0 | 可删除 ZeroTier 网络 |
| T4.8 | 更新 network object | P0 | 可修改名称、路由、DNS 等 |
| T4.9 | 获取 members | P0 | 成员列表可展示 |
| T4.10 | 更新 member object | P0 | 可授权、桥接、分配 IP |
| T4.11 | 删除 member | P0 | 可删除成员 |
| T4.12 | 获取 peers | P0 | 可判断在线状态 |
| T4.13 | ZeroTier model 映射 HomeMesh model | P0 | UI 不依赖 ZeroTier 原始模型 |
| T4.14 | Provider 连接测试 API | P0 | 配置后可测试 |

## 7. Epic 5：网络管理 API

目标：完成 ztncui 网络能力替代。

| 编号 | 任务 | 优先级 | 验收标准 |
|---|---|---:|---|
| T5.1 | 网络列表 API | P0 | 返回本地 + Provider 状态 |
| T5.2 | 网络详情 API | P0 | 返回详情、成员、路由摘要 |
| T5.3 | 创建网络 API | P0 | 创建本地 Network 和 Provider Network |
| T5.4 | 删除网络 API | P0 | 删除本地和 Provider 网络 |
| T5.5 | 修改网络名称 API | P0 | 同步到 Provider |
| T5.6 | Private 开关 API | P0 | 同步到 Provider |
| T5.7 | v4AssignMode API | P0 | 同步到 Provider |
| T5.8 | v6AssignMode API | P1 | 可保存和同步 |
| T5.9 | Easy Setup API | P0 | 自动配置 CIDR、IP 池、路由 |
| T5.10 | Provider 状态同步 API | P0 | 手动刷新可用 |

## 8. Epic 6：成员管理 API

目标：完成 ztncui 成员能力替代。

| 编号 | 任务 | 优先级 | 验收标准 |
|---|---|---:|---|
| T6.1 | 成员列表 API | P0 | 可查看所有成员 |
| T6.2 | 成员详情 API | P0 | 可查看成员详情 |
| T6.3 | 授权成员 API | P0 | 可授权待加入设备 |
| T6.4 | 取消授权 API | P0 | 可取消授权 |
| T6.5 | 设置成员名称 API | P0 | 保存本地扩展名称 |
| T6.6 | 删除成员 API | P0 | 从 Provider 删除成员 |
| T6.7 | 分配固定 IP API | P0 | 可添加 IP |
| T6.8 | 删除固定 IP API | P0 | 可删除 IP |
| T6.9 | Active Bridge API | P0 | 可开启/关闭桥接 |
| T6.10 | 成员标签 API | P1 | 可保存标签 |
| T6.11 | 成员角色 API | P1 | 可设置 Gateway / Service 等 |

## 9. Epic 7：路由 / IP 池 / DNS

目标：完成网络配置闭环。

| 编号 | 任务 | 优先级 | 验收标准 |
|---|---|---:|---|
| T7.1 | 路由列表 API | P0 | 可展示 routes |
| T7.2 | 新增路由 API | P0 | 可新增 CIDR 路由 |
| T7.3 | 删除路由 API | P0 | 可删除路由 |
| T7.4 | CIDR 校验 | P0 | 非法 CIDR 被拒绝 |
| T7.5 | via IP 校验 | P0 | 非法 via 被拒绝 |
| T7.6 | IP 池列表 API | P0 | 可展示 IP 池 |
| T7.7 | 新增 IP 池 API | P0 | 可新增 IP 范围 |
| T7.8 | 删除 IP 池 API | P0 | 可删除 IP 范围 |
| T7.9 | DNS 查看 API | P0 | 可查看 DNS 配置 |
| T7.10 | DNS 修改 API | P0 | 可修改 domain 和 servers |

## 10. Epic 8：设备接入 Enrollment

目标：实现 HomeMesh 相比 ztncui 的差异化接入能力。

| 编号 | 任务 | 优先级 | 验收标准 |
|---|---|---:|---|
| T8.1 | 生成邀请 Token | P0 | 可生成邀请码 |
| T8.2 | 邀请 Token 有效期 | P0 | 过期不可用 |
| T8.3 | 邀请 Token 指定 Network | P0 | 设备知道加入哪个网络 |
| T8.4 | 邀请码展示 | P0 | 页面可复制邀请码 |
| T8.5 | 二维码生成 | P1 | 页面可扫码 |
| T8.6 | 设备 enroll API | P0 | 客户端可提交入网请求 |
| T8.7 | 自动创建 Device | P0 | enroll 后创建设备 |
| T8.8 | 自动绑定 Member | P1 | 与待授权成员关联 |
| T8.9 | 自动授权开关 | P1 | 可按邀请配置自动授权 |
| T8.10 | 撤销邀请 | P1 | 邀请可撤销 |

## 11. Epic 9：Web 管理后台

目标：实现原型图对应的核心页面。

| 编号 | 页面 | 优先级 | 验收标准 |
|---|---|---:|---|
| T9.1 | 登录页 | P0 | 可登录进入后台 |
| T9.2 | 控制台总览 | P0 | KPI 和最近事件可展示 |
| T9.3 | 家庭网络列表 | P0 | 网络列表可查看 |
| T9.4 | 网络详情 - 概览 | P0 | 展示网络摘要 |
| T9.5 | 网络详情 - 成员 | P0 | 成员表可操作 |
| T9.6 | 网络详情 - 路由 | P0 | 路由可增删 |
| T9.7 | 网络详情 - IP 池 | P0 | IP 池可增删 |
| T9.8 | 网络详情 - DNS | P0 | DNS 可修改 |
| T9.9 | 设备接入页面 | P0 | 可生成邀请码 |
| T9.10 | Provider 页面 | P0 | 展示 ZeroTier 状态 |
| T9.11 | 网关页面 | P1 | 可查看网关配置 |
| T9.12 | ACL 页面 | P1 | 可展示策略预留 |
| T9.13 | 审计日志页面 | P1 | 可查看日志 |

## 12. Epic 10：审计与日志

目标：所有关键操作可追溯。

| 编号 | 任务 | 优先级 | 验收标准 |
|---|---|---:|---|
| T10.1 | 登录日志 | P0 | 登录成功/失败记录 |
| T10.2 | 网络操作日志 | P0 | 创建/删除/修改记录 |
| T10.3 | 成员授权日志 | P0 | 授权/取消授权记录 |
| T10.4 | 路由修改日志 | P0 | 新增/删除路由记录 |
| T10.5 | DNS 修改日志 | P0 | DNS 修改记录 |
| T10.6 | Provider 异常日志 | P0 | 连接失败记录 |
| T10.7 | 审计日志查询 API | P1 | 可分页查询 |
| T10.8 | 审计日志页面 | P1 | 可筛选查看 |

## 13. Epic 11：部署与交付

目标：第一阶段可被真实部署测试。

| 编号 | 任务 | 优先级 | 验收标准 |
|---|---|---:|---|
| T11.1 | Dockerfile | P0 | 可构建镜像 |
| T11.2 | docker-compose.yml | P0 | 可一键启动 |
| T11.3 | Linux systemd 示例 | P1 | 可作为服务运行 |
| T11.4 | 配置文件模板 | P0 | 可配置 ZeroTier API |
| T11.5 | README 安装文档 | P0 | 新用户可按文档启动 |
| T11.6 | 数据备份说明 | P1 | SQLite 备份方式明确 |
| T11.7 | Migration 升级说明 | P1 | 数据库升级路径明确 |

## 14. 推荐 Sprint 排期

### Sprint 1：基础骨架

- 项目结构
- SQLite / EF Core
- 管理员初始化
- Swagger
- Docker
- Health Check

### Sprint 2：ZeroTier Provider

- 读取 token
- 获取 status
- networks
- members
- peers
- Provider 页面

### Sprint 3：网络管理

- 网络 CRUD
- Easy Setup
- routes
- ip pools
- DNS

### Sprint 4：成员管理

- 成员列表
- 授权/取消授权
- 命名
- IP 分配
- Active Bridge

### Sprint 5：HomeMesh 增强

- Device 模型
- Enrollment Token
- Gateway 角色
- 标签
- 审计日志

### Sprint 6：打磨与测试

- UI 打磨
- 表单校验
- 错误提示
- Docker 部署测试
- README
- 单元测试

## 15. 第一阶段完成定义

当以下条件全部满足，即认为 `v0.1 Server MVP` 完成：

- 可以通过 Docker 启动
- 可以初始化管理员并登录
- 可以连接本机 ZeroTier One
- 可以创建、查看、删除 ZeroTier 网络
- 可以执行 Easy Setup
- 可以查看、授权、取消授权成员
- 可以给成员分配固定 IP
- 可以新增 / 删除路由
- 可以配置 DNS domain 和 servers
- 关键操作有审计日志
- Swagger 可用
- 核心页面可操作
