# ─────────────────────────────────────────────────────────────────────────────
# 阶段1：在 Debian 环境编译 mkworld（自定义 planet 文件生成工具）
# mkworld_custom.cpp 来自 xubiaolin，纯 C++，只依赖 zerotier 内部头文件
# ─────────────────────────────────────────────────────────────────────────────
FROM debian:bookworm-slim AS mkworld-builder

# 仅保留 bookworm 主源，移除 updates/security（这些在部分网络返回 400）
# 对于编译构建阶段，主源已足够
# debian:bookworm-slim 无 ca-certificates，只能用 HTTP 协议；选中科大源（USTC）
RUN printf 'deb http://mirrors.ustc.edu.cn/debian bookworm main\n' \
        > /etc/apt/sources.list \
    && rm -f /etc/apt/sources.list.d/*.sources \
    && rm -f /etc/apt/sources.list.d/*.list \
    && printf 'Acquire::http::Pipeline-Depth "0";\nAcquire::Retries "5";\n' \
        > /etc/apt/apt.conf.d/99no-pipeline

RUN apt-get update && apt-get install -y --no-install-recommends \
        build-essential cmake curl ca-certificates \
    && rm -rf /var/lib/apt/lists/*

# 使用宿主机 git sparse-checkout 预下载的 ZeroTierOne 1.12.2 源码（只含编译所需目录）
# 宿主机准备命令：
#   git clone --depth 1 --filter=blob:none --sparse -b 1.12.2 https://github.com/zerotier/ZeroTierOne.git deploy/docker/zt-sparse
#   cd deploy/docker/zt-sparse && git sparse-checkout set node osdep include attic/world
# attic/world/ 在 1.12.x 中存在，1.14+ 已删除；ext/ 目录含预编译库（约 300MB）不需要也不复制
COPY deploy/docker/zt-sparse/node /zt/node
COPY deploy/docker/zt-sparse/osdep /zt/osdep
COPY deploy/docker/zt-sparse/include /zt/include
COPY deploy/docker/zt-sparse/attic /zt/attic
COPY deploy/docker/zt-sparse/ext/nlohmann /zt/ext/nlohmann

# 替换为 xubiaolin 的自定义 mkworld.cpp（支持从 JSON 动态生成 planet 文件）
RUN curl -fsSL \
    https://raw.githubusercontent.com/xubiaolin/docker-zerotier-planet/master/patch/mkworld_custom.cpp \
    -o /zt/attic/world/mkworld.cpp

RUN cd /zt/attic/world && \
    c++ -std=c++11 -I../.. -I../../ext -I.. -g -o mkworld \
        ../../node/C25519.cpp \
        ../../node/Salsa20.cpp \
        ../../node/SHA512.cpp \
        ../../node/Identity.cpp \
        ../../node/Utils.cpp \
        ../../node/InetAddress.cpp \
        ../../osdep/OSUtils.cpp \
        mkworld.cpp \
        -lm

# ─────────────────────────────────────────────────────────────────────────────
# 最终镜像：基于 .NET 10 官方镜像（Debian Bookworm，glibc，与阶段2兼容）
# 包含：zerotier-one（官方包）+ mkworld（阶段2编译）+ HomeMesh WebApi
#
# 宿主机预构建：.\deploy\build.ps1
# 启动：docker compose -f deploy\docker\docker-compose.yml up -d --build
# ─────────────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0

ARG TZ=Asia/Shanghai
ENV TZ=${TZ}
ENV ASPNETCORE_URLS=http://+:8080

# 禁用 HTTP 流水线（绕过 Docker Desktop 代理 400 问题）+ 替换国内镜像源
# 兼容 Ubuntu DEB822 格式 (ubuntu.sources) / Debian DEB822 (debian.sources) / 旧格式 (sources.list)
RUN printf 'Acquire::http::Pipeline-Depth "0";\nAcquire::Retries "5";\n' \
        > /etc/apt/apt.conf.d/99no-pipeline; \
    if [ -f /etc/apt/sources.list.d/ubuntu.sources ]; then \
        sed -i \
            -e 's|http://archive.ubuntu.com/ubuntu|http://mirrors.ustc.edu.cn/ubuntu|g' \
            -e 's|https://archive.ubuntu.com/ubuntu|http://mirrors.ustc.edu.cn/ubuntu|g' \
            -e 's|http://security.ubuntu.com/ubuntu|http://mirrors.ustc.edu.cn/ubuntu|g' \
            -e 's|https://security.ubuntu.com/ubuntu|http://mirrors.ustc.edu.cn/ubuntu|g' \
            /etc/apt/sources.list.d/ubuntu.sources; \
    fi; \
    if [ -f /etc/apt/sources.list.d/debian.sources ]; then \
        sed -i \
            -e 's|http://deb.debian.org/debian|http://mirrors.ustc.edu.cn/debian|g' \
            -e 's|http://security.debian.org/debian-security|http://mirrors.ustc.edu.cn/debian-security|g' \
            /etc/apt/sources.list.d/debian.sources; \
    fi; \
    if [ -f /etc/apt/sources.list ]; then \
        sed -i \
            -e 's|http://deb.debian.org/debian|http://mirrors.ustc.edu.cn/debian|g' \
            -e 's|http://security.debian.org/debian-security|http://mirrors.ustc.edu.cn/debian-security|g' \
            -e 's|http://archive.ubuntu.com/ubuntu|http://mirrors.ustc.edu.cn/ubuntu|g' \
            -e 's|http://security.ubuntu.com/ubuntu|http://mirrors.ustc.edu.cn/ubuntu|g' \
            /etc/apt/sources.list; \
    fi

RUN apt-get update \
    && DEBIAN_FRONTEND=noninteractive apt-get install -y --no-install-recommends \
        tzdata ca-certificates curl jq iproute2 \
    && rm -rf /var/lib/apt/lists/*

# 安装 ZeroTier（官方脚本，Debian/Ubuntu 适用）
# 安装后立即清理构建期间产生的身份文件，使容器首次启动时能通过 entrypoint 重新初始化
RUN curl -fsSL https://install.zerotier.com | bash; \
    test -x /usr/sbin/zerotier-one || { echo "ZeroTier 安装失败"; exit 1; }; \
    rm -rf /var/lib/zerotier-one/*

# 拷贝 mkworld 二进制（阶段2编译，glibc，与最终镜像兼容）
COPY --from=mkworld-builder /zt/attic/world/mkworld /usr/local/bin/mkworld
RUN chmod +x /usr/local/bin/mkworld

# 创建运行时所需目录
RUN mkdir -p /app/dist /app/config /app/data /app/homemesh /var/lib/zerotier-one

# 拷贝 HomeMesh WebApi 发布产物（由宿主机 build.ps1 预先构建）
COPY artifacts/publish/webapi /app/homemesh/

# 拷贝 entrypoint（planet 初始化 + HomeMesh WebApi 启动）
COPY deploy/docker/entrypoint.sh /app/entrypoint.sh
RUN chmod +x /app/entrypoint.sh

EXPOSE 8080
EXPOSE 9993/udp
EXPOSE 9993

ENTRYPOINT ["/bin/sh", "/app/entrypoint.sh"]
