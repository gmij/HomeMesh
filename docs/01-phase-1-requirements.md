# 第一阶段需求分析

## 1. 阶段定位

第一阶段目标是构建 `HomeMesh Controller v0.1 Server MVP`。

该版本的核心定位：

> 功能上替代 ztncui，架构上升级为可扩展的 Home SD-WAN 控制面。

第一阶段只实现 ZeroTier Provider，但所有领域模型、服务接口和 API 都不应写死 ZeroTier。

## 2. 第一阶段目标

- 使用 .NET 10 构建服务端控制面
- 替代 ztncui 的 ZeroTier Controller 管理能力
- 建立 HomeMesh 自己的领域模型
- 提供现代化 Web Admin UI
- 提供 REST API 和 OpenAPI 文档
- 通过 Provider 抽象预留多协议扩展能力
- 支持 Docker / Linux 部署
- 引入审计日志与设备接入邀请机制

## 3. 第一阶段范围

### 3.1 必须完成

- 管理员初始化与登录
- Dashboard 总览
- Provider 管理
- ZeroTier Provider
- 网络管理
- 成员管理
- 路由管理
- IP 池管理
- DNS 管理
- 设备接入 Enrollment MVP
- 审计日志
- Docker 部署
- OpenAPI / Swagger

### 3.2 暂不完成

- Windows 客户端改造
- Android 客户端改造
- iOS 客户端
- WireGuard Provider 真正实现
- 自研 QUIC 协议
- 云中继
- 收费系统
- 完整 ACL 执行
- 企业级多租户

## 4. 核心用户

### 4.1 管理员

家庭网络拥有者，可管理网络、设备、成员、路由、DNS、网关和审计日志。

### 4.2 普通设备

真实终端设备，例如 Windows、Linux、Android、NAS、摄像头、软路由等。

### 4.3 网关设备

具备路由能力的设备，未来承担家庭 LAN 子网路由、出口节点、旁路由、DNS 转发等能力。

## 5. 核心业务概念

- Home：家庭空间
- Network：HomeMesh 虚拟网络
- ProviderBinding：协议绑定关系
- Device：真实设备
- Member：设备在某个虚拟网络中的成员身份
- Gateway：具备路由能力的设备
- Route：路由配置
- IpPool：IP 分配池
- DnsConfig：DNS 配置
- EnrollmentToken：设备邀请令牌
- AuditLog：审计日志

## 6. 功能需求

### 6.1 Dashboard

- 显示控制器状态
- 显示 Provider 状态
- 显示网络数量
- 显示设备数量
- 显示在线设备数
- 显示网关节点数量
- 显示最近事件
- 显示待授权设备

### 6.2 Provider 管理

- 显示已配置 Provider
- 显示 ZeroTier Provider 状态
- 支持配置 ZeroTier API 地址
- 支持读取 authtoken.secret
- 支持测试连接
- 预留 WireGuard / Custom QUIC Provider 展示位

### 6.3 网络管理

- 网络列表
- 网络详情
- 创建网络
- 删除网络
- 修改网络名称
- Private 开关
- IPv4 分配模式
- IPv6 分配模式预留
- Easy Setup
- 与 Provider 状态同步

### 6.4 成员管理

- 成员列表
- 成员详情
- 授权 / 取消授权
- 成员命名
- 成员删除
- 固定 IP 分配
- 固定 IP 删除
- Active Bridge 开关
- 在线状态展示
- 设备标签预留
- 设备角色预留

### 6.5 路由管理

- 路由列表
- 新增路由
- 删除路由
- CIDR 校验
- via IP 校验
- 家庭 LAN 路由语义预留
- 出口节点路由语义预留

### 6.6 IP 池管理

- 查看 IP 池
- 新增 IP 池
- 删除 IP 池
- 校验 IP 范围
- 校验是否落在网络 CIDR 内
- 防止范围冲突预留

### 6.7 DNS 管理

- 查看 DNS 配置
- 修改 DNS domain
- 修改 DNS servers
- 校验 DNS server IP
- Split DNS 模型预留

### 6.8 设备接入 Enrollment

- 生成邀请 Token
- 设置有效期
- 指定加入网络
- 指定默认标签预留
- 自动授权开关预留
- 显示邀请码
- 显示二维码预留
- 撤销邀请预留
- 设备 enroll API

### 6.9 网关节点

- 将成员标记为 Gateway
- 配置 LAN Subnet
- 标记 Subnet Router
- 标记 Exit Node
- 显示健康状态预留

### 6.10 ACL 策略

第一阶段只做模型和 UI 预留，不做强执行。

- 策略列表
- 策略模型
- 按设备授权预留
- 按标签授权预留
- 按端口授权预留

### 6.11 审计日志

- 登录日志
- 网络创建日志
- 成员授权日志
- 路由修改日志
- DNS 修改日志
- Provider 异常日志
- 审计日志查询 API

## 7. 第一阶段验收标准

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
