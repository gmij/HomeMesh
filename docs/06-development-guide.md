# 开发启动说明

## 1. 环境要求

- .NET 10 SDK
- Docker / Docker Compose，可选
- ZeroTier One，可选，第一阶段当前 Provider 仍是占位实现

## 2. 本地启动

```bash
dotnet restore HomeMesh.sln
dotnet run --project src/HomeMesh.WebApi/HomeMesh.WebApi.csproj
```

默认地址：

```text
http://localhost:5000
```

Swagger：

```text
http://localhost:5000/swagger
```

健康检查：

```text
GET /health
```

## 3. 初始化管理员

检查初始化状态：

```bash
curl http://localhost:5000/api/setup/status
```

初始化管理员：

```bash
curl -X POST http://localhost:5000/api/setup/admin \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"change-me"}'
```

初始化后会自动创建默认 Home。

## 4. 创建网络

```bash
curl -X POST http://localhost:5000/api/networks \
  -H "Content-Type: application/json" \
  -d '{"name":"主家庭网络","provider":"ZeroTier","cidr":"10.10.0.0/24","private":true}'
```

当前 ZeroTier Provider 是占位实现，会生成 stub Provider Network ID。下一阶段会接入真实 ZeroTier Local API。

## 5. 查看 Provider 状态

```bash
curl http://localhost:5000/api/providers
curl http://localhost:5000/api/providers/ZeroTier/status
```

## 6. Docker 启动

```bash
docker compose -f deploy/docker/docker-compose.yml up -d --build
```

访问：

```text
http://localhost:8080/swagger
```

## 7. 当前接口

- `GET /health`
- `GET /api/setup/status`
- `POST /api/setup/admin`
- `GET /api/providers`
- `GET /api/providers/{providerName}/status`
- `GET /api/networks`
- `POST /api/networks`
- `GET /api/audit-logs`

`NetworkEndpoints.cs` 已经预留了网络详情和删除接口，待入口文件清理后统一挂载。

## 8. 下一步开发重点

1. 接入真实 ZeroTier Local API
2. 完成网络详情与删除接口挂载
3. 实现 Route / IP Pool / DNS 服务层
4. 实现 Member 服务层
5. 接入 Web Admin 前端
