#!/bin/sh
set -x

# ─── 路径配置 ────────────────────────────────────────────────────────────────
ZEROTIER_PATH="/var/lib/zerotier-one"
APP_PATH="/app"
CONFIG_PATH="${APP_PATH}/config"

ZT_PORT="${ZT_PORT:-9993}"
ZT_PUBLIC_PORT="${ZT_PUBLIC_PORT:-${ZT_PORT}}"

# ─── 首次初始化 ZeroTier 数据（生成自定义 planet 文件）────────────────────────
# 与 xubiaolin 的初始化逻辑相同，但使用系统安装的 zerotier 命令（非 ./zerotier-* 相对路径）
generate_planet_files() {
    cd "${ZEROTIER_PATH}"
    zerotier-idtool initmoon identity.public > moon.json

    # 获取公网 IP（环境变量优先，否则自动探测）
    IP_ADDR4="${IP_ADDR4:-$(curl -s --max-time 10 https://ipv4.icanhazip.com/)}"
    IP_ADDR6="${IP_ADDR6:-$(curl -s --max-time 10 https://ipv6.icanhazip.com/)}"
    echo "IP_ADDR4=${IP_ADDR4}"
    echo "IP_ADDR6=${IP_ADDR6}"

    if [ -z "${IP_ADDR4}" ] && [ -z "${IP_ADDR6}" ]; then
        echo "错误：无法获取公网 IP，请通过环境变量 IP_ADDR4 手动指定"
        exit 1
    fi

    # 构造 stableEndpoints JSON
    if [ -n "${IP_ADDR4}" ] && [ -n "${IP_ADDR6}" ]; then
        stableEndpoints="[\"${IP_ADDR4}/${ZT_PUBLIC_PORT}\",\"${IP_ADDR6}/${ZT_PUBLIC_PORT}\"]"
    elif [ -n "${IP_ADDR4}" ]; then
        stableEndpoints="[\"${IP_ADDR4}/${ZT_PUBLIC_PORT}\"]"
    else
        stableEndpoints="[\"${IP_ADDR6}/${ZT_PUBLIC_PORT}\"]"
    fi
    echo "stableEndpoints=${stableEndpoints}"

    echo "${IP_ADDR4}" > "${CONFIG_PATH}/ip_addr4"
    echo "${IP_ADDR6}" > "${CONFIG_PATH}/ip_addr6"

    # 将 stableEndpoints 写入 moon.json，生成 moon 文件
    jq --argjson ep "${stableEndpoints}" \
        '.roots[0].stableEndpoints = $ep' moon.json > temp.json \
        && mv temp.json moon.json

    zerotier-idtool genmoon moon.json
    mkdir -p moons.d
    cp ./*.moon moons.d/

    # 调用 mkworld 生成自定义 planet 文件（world.bin）
    mkworld
    if [ $? -ne 0 ]; then
        echo "错误：mkworld 执行失败"
        exit 1
    fi

    mkdir -p "${APP_PATH}/dist/"
    mv world.bin "${APP_PATH}/dist/planet"
    cp ./*.moon "${APP_PATH}/dist/"
    echo "==> planet 文件生成成功：${APP_PATH}/dist/planet"

    echo "${ZT_PUBLIC_PORT}" > "${CONFIG_PATH}/zerotier-public.port"
}

# ─── 首次初始化 ZeroTier 数据（生成自定义 planet 文件）────────────────────────
# 与 xubiaolin 的初始化逻辑相同，但使用系统安装的 zerotier 命令（非 ./zerotier-* 相对路径）
init_zerotier_data() {
    echo "==> 首次初始化 ZeroTier 数据..."

    echo "${ZT_PORT}" > "${CONFIG_PATH}/zerotier-one.port"

    cd "${ZEROTIER_PATH}"

    # 生成 authtoken、身份
    openssl rand -hex 16 > authtoken.secret
    zerotier-idtool generate identity.secret identity.public

    generate_planet_files
}

# ─── 检查并初始化 ZeroTier ──────────────────────────────────────────────────
# 使用 zerotier-one.port 文件作为"初始化已完成"标志：
# - init_zerotier_data 会写入此文件（和自定义 planet）
# - 检查目录是否为空的方式不可靠（镜像内置 ZT 数据会被 Docker 复制到空 volume）
check_zerotier() {
    mkdir -p "${ZEROTIER_PATH}" "${CONFIG_PATH}"
    if [ -f "${CONFIG_PATH}/zerotier-one.port" ]; then
        echo "${CONFIG_PATH}/zerotier-one.port 已存在，跳过初始化"

        CURRENT_PUBLIC_PORT=""
        if [ -f "${CONFIG_PATH}/zerotier-public.port" ]; then
            CURRENT_PUBLIC_PORT="$(cat "${CONFIG_PATH}/zerotier-public.port")"
        fi

        if [ "${CURRENT_PUBLIC_PORT}" != "${ZT_PUBLIC_PORT}" ]; then
            echo "检测到 ZT_PUBLIC_PORT 变化（当前: ${CURRENT_PUBLIC_PORT:-<empty>}，目标: ${ZT_PUBLIC_PORT}），重生成 planet/moon..."
            if [ -f "${ZEROTIER_PATH}/identity.public" ]; then
                generate_planet_files
            else
                echo "identity.public 不存在，执行首次初始化..."
                init_zerotier_data
            fi
        fi
    else
        echo "${CONFIG_PATH}/zerotier-one.port 不存在，执行首次初始化..."
        init_zerotier_data
    fi
}

# ─── 启动所有服务 ────────────────────────────────────────────────────────────
# 启动顺序：zerotier-one → HomeMesh WebApi（替代 ztncui）
start() {
    # zerotier-one.port 可能因 init 被跳过（volume 中已有镜像内置数据）而不存在，回退到 ZT_PORT 环境变量
    if [ -f "${CONFIG_PATH}/zerotier-one.port" ]; then
        ZT_P=$(cat "${CONFIG_PATH}/zerotier-one.port")
    else
        ZT_P="${ZT_PORT:-9993}"
        echo "${ZT_P}" > "${CONFIG_PATH}/zerotier-one.port"
        echo "zerotier-one.port 不存在，使用环境变量 ZT_PORT=${ZT_P}"
    fi
    zerotier-one -p"${ZT_P}" -d || exit 1

    echo "==> 启动 HomeMesh WebApi..."
    # 动态注入 ZeroTier API 地址（与实际启动端口保持一致）
    export Providers__ZeroTier__Port="${ZT_P}"

    cd /app/homemesh
    exec dotnet HomeMesh.WebApi.dll
}

# ─── 主流程 ──────────────────────────────────────────────────────────────────
mkdir -p "${CONFIG_PATH}"
check_zerotier
start
