# HomeMesh v0.1 MVP Checklist

本文用于确认 HomeMesh v0.1 Server MVP 是否已经具备可演示、可试跑、可继续迭代的最小闭环。

## 1. 启动与部署

- [x] .NET 10 WebApi 可启动
- [x] SQLite 数据库自动创建
- [x] SQLite 数据库目录自动创建
- [x] Dockerfile 可构建 WebApi 镜像
- [x] docker-compose 提供基础部署配置
- [x] docker-compose 暴露 `8080` 端口
- [x] docker-compose 支持配置 ZeroTier Provider 环境变量
- [x] README 提供本地运行和 Docker 运行说明

## 2. 初始化与认证

- [x] 首次启动可创建管理员账号
- [x] 管理员密码使用 BCrypt Hash
- [x] 登录后写入 HttpOnly Session Cookie
- [x] 支持退出登录
- [x] 业务 API 默认需要登录态
- [x] 登录过期时前端自动回登录页

## 3. Provider 抽象

- [x] 定义 `ISdwanControllerProvider`
- [x] 支持 Provider 健康检查
- [x] 支持创建、读取、更新、删除虚拟网络
- [x] 支持成员列表同步
- [x] 支持成员授权状态更新
- [x] 支持网络配置同步

## 4. ZeroTier Provider

- [x] 接入 ZeroTier Local API
- [x] 支持读取 `authtoken.secret`
- [x] 支持 ZeroTier 状态检查
- [x] 支持 ZeroTier Controller 网络创建
- [x] 支持 ZeroTier 网络配置更新
- [x] 支持 ZeroTier 成员同步
- [x] 支持 ZeroTier 成员授权/取消授权

## 5. Demo Provider

- [x] 内置 Demo Provider
- [x] 无 ZeroTier 环境也可创建演示网络
- [x] Demo Provider 支持演示成员同步
- [x] Demo Provider 支持演示授权/取消授权
- [x] Demo Provider 支持演示配置同步

## 6. 网络配置能力

- [x] 创建网络
- [x] 删除网络
- [x] Easy Setup 写入 CIDR、IP 池、DNS
- [x] 添加路由
- [x] 删除路由
- [x] 添加 IP 池
- [x] 删除 IP 池
- [x] 保存 DNS 配置
- [x] 同步网络配置到 Provider
- [x] 查看配置同步状态

## 7. 成员管理能力

- [x] 从 Provider 同步成员
- [x] 查看成员列表
- [x] 查看成员在线状态
- [x] 授权成员
- [x] 取消授权成员
- [x] 查看成员同步状态

## 8. Web Admin 管理台

- [x] `/` 内置管理台
- [x] 初始化管理员页面
- [x] 登录页面
- [x] Dashboard 指标卡
- [x] Provider 状态显示
- [x] Provider 连接失败友好提示
- [x] 网络创建表单
- [x] Provider 选择
- [x] Easy Setup 表单
- [x] 路由/IP 池/DNS 表单
- [x] 成员授权操作
- [x] 审计日志展示

## 9. 审计与可观测性

- [x] 关键操作写入 AuditLog
- [x] Dashboard Summary API
- [x] 最近审计日志 API
- [x] 前端展示审计日志
- [x] Serilog 控制台日志

## 10. CI 与测试

- [x] GitHub Actions CI
- [x] Restore / Build / Test
- [x] Docker build job
- [x] 基础单元测试
- [x] WebApi Smoke Tests
- [x] `/health` 测试
- [x] 未登录业务 API 返回 401 测试
- [x] `/api/auth/status` 公开可访问测试

## 11. v0.1 暂不包含

- [ ] 多用户、多租户权限模型
- [ ] 持久化 Session / Refresh Token
- [ ] WireGuard Provider
- [ ] Headscale Provider
- [ ] 自研 QUIC 协议
- [ ] 完整 ACL Enforcement
- [ ] 客户端 Agent
- [ ] 正式前端工程化构建链
- [ ] 数据库 Migration 管理
- [ ] HTTPS 证书自动配置

## 12. MVP 演示路径

### Demo 模式

1. 启动 HomeMesh WebApi
2. 打开 `/`
3. 创建管理员
4. 登录
5. 创建网络时选择 `Demo 演示`
6. 使用 Easy Setup 配置 CIDR/IP 池/DNS
7. 点击“同步配置”
8. 点击“同步成员”
9. 在成员列表中授权/取消授权演示设备
10. 查看 Dashboard 和审计日志

### ZeroTier 模式

1. 确认 ZeroTier One 正在运行
2. 确认 Local API 可访问
3. 确认 `authtoken.secret` 可读
4. 在 docker-compose 或环境变量中配置 ZeroTier Provider
5. 创建网络时选择 `ZeroTier`
6. 使用 Easy Setup 配置网络
7. 同步配置
8. 设备加入网络后同步成员并授权

## 13. 下一阶段建议

1. 引入正式前端工程：React / Vite / Tailwind
2. 将 Session 持久化到数据库
3. 引入数据库 Migration
4. 完善错误码与 API Response 规范
5. 增加 ACL 策略模型 UI
6. 增加 Enrollment Token 和设备接入流程
7. 增加 WireGuard / Headscale Provider
8. 增加 OpenAPI 文档分组和 API Key 支持
