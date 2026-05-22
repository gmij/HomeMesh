

# ─────────────────────────────────────────────────────────────────────────────
# 阶段2：在 Debian 环境编译 mkworld（自定义 planet 文件生成工具）
# mkworld_custom.cpp 来自 xubiaolin，纯 C++，只依赖 zerotier 内部头文件
# ─────────────────────────────────────────────────────────────────────────────
FROM debian:bookworm-slim AS mkworld-builder

# debian:bookworm-slim 无 ca-certificates，只能用 HTTP 协议；选中科大源（USTC）
RUN printf 'deb http://mirrors.ustc.edu.cn/debian bookworm main\n' \
        > /etc/apt/sources.list \
    && rm -f /etc/apt/sources.list.d/*.sources \
    && rm -f /etc/apt/sources.list.d/*.list \
    && printf 'Acquire::http::Pipeline-Depth "0";\nAcquire::Retries "5";\n' \
        > /etc/apt/apt.conf.d/99no-pipeline

RUN apt-get update && apt-get install -y --no-install-recommends \
        build-essential cmake git curl ca-certificates \
    && rm -rf /var/lib/apt/lists/*
