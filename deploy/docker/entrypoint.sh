#!/bin/sh
set -eu

APP_PATH="/app"
APP_DATA_PATH="${APP_PATH}/data"
ARTIFACTS_PATH="${APP_DATA_PATH}/artifacts"
ZEROTIER_PATH="/var/lib/zerotier-one"

ZT_PORT="${ZT_PORT:-9993}"
ZT_PUBLIC_PORT="${ZT_PUBLIC_PORT:-${ZT_PORT}}"
ZT_PORT_FILE="${ZEROTIER_PATH}/zerotier-one.port"
ZT_PUBLIC_PORT_FILE="${ZEROTIER_PATH}/homemesh-public.port"

generate_planet_files() {
    cd "${ZEROTIER_PATH}"
    zerotier-idtool initmoon identity.public > moon.json

    DETECTED_IP4="$(curl -fsS --max-time 10 https://ipv4.icanhazip.com/ 2>/dev/null || true)"
    DETECTED_IP6="$(curl -fsS --max-time 10 https://ipv6.icanhazip.com/ 2>/dev/null || true)"
    IP_ADDR4="${IP_ADDR4:-${DETECTED_IP4}}"
    IP_ADDR6="${IP_ADDR6:-${DETECTED_IP6}}"

    if [ -z "${IP_ADDR4}" ] && [ -z "${IP_ADDR6}" ]; then
        IP_ADDR4="127.0.0.1"
        echo "No public IP detected, falling back to ${IP_ADDR4} for local development."
    fi

    if [ -n "${IP_ADDR4}" ] && [ -n "${IP_ADDR6}" ]; then
        STABLE_ENDPOINTS="[\"${IP_ADDR4}/${ZT_PUBLIC_PORT}\",\"${IP_ADDR6}/${ZT_PUBLIC_PORT}\"]"
    elif [ -n "${IP_ADDR4}" ]; then
        STABLE_ENDPOINTS="[\"${IP_ADDR4}/${ZT_PUBLIC_PORT}\"]"
    else
        STABLE_ENDPOINTS="[\"${IP_ADDR6}/${ZT_PUBLIC_PORT}\"]"
    fi

    jq --argjson ep "${STABLE_ENDPOINTS}" \
        '.roots[0].stableEndpoints = $ep' moon.json > moon.json.tmp \
        && mv moon.json.tmp moon.json

    zerotier-idtool genmoon moon.json
    mkdir -p moons.d "${ARTIFACTS_PATH}"
    cp ./*.moon moons.d/

    if ! mkworld >/tmp/homemesh-mkworld.log 2>&1; then
        cat /tmp/homemesh-mkworld.log >&2
        exit 1
    fi
    rm -f /tmp/homemesh-mkworld.log

    rm -f "${ARTIFACTS_PATH}/planet" "${ARTIFACTS_PATH}"/*.moon
    mv world.bin "${ARTIFACTS_PATH}/planet"
    cp ./*.moon "${ARTIFACTS_PATH}/"
    echo "${ZT_PUBLIC_PORT}" > "${ZT_PUBLIC_PORT_FILE}"
}

init_zerotier_data() {
    echo "Initializing ZeroTier data..."
    echo "${ZT_PORT}" > "${ZT_PORT_FILE}"

    cd "${ZEROTIER_PATH}"
    openssl rand -hex 16 > authtoken.secret
    zerotier-idtool generate identity.secret identity.public

    generate_planet_files
}

check_zerotier() {
    mkdir -p "${ZEROTIER_PATH}" "${APP_DATA_PATH}" "${ARTIFACTS_PATH}"

    if [ -f "${ZT_PORT_FILE}" ]; then
        CURRENT_PORT="$(cat "${ZT_PORT_FILE}")"
        CURRENT_PUBLIC_PORT=""
        if [ -f "${ZT_PUBLIC_PORT_FILE}" ]; then
            CURRENT_PUBLIC_PORT="$(cat "${ZT_PUBLIC_PORT_FILE}")"
        fi

        if [ "${CURRENT_PORT}" != "${ZT_PORT}" ]; then
            echo "ZT_PORT changed from ${CURRENT_PORT} to ${ZT_PORT}, updating ZeroTier listen port..."
            echo "${ZT_PORT}" > "${ZT_PORT_FILE}"
        fi

        if [ "${CURRENT_PUBLIC_PORT}" != "${ZT_PUBLIC_PORT}" ]; then
            echo "ZT_PUBLIC_PORT changed, regenerating planet and moon artifacts..."
            if [ -f "${ZEROTIER_PATH}/identity.public" ]; then
                generate_planet_files
            else
                init_zerotier_data
            fi
        fi
    else
        init_zerotier_data
    fi
}

start() {
    if [ -f "${ZT_PORT_FILE}" ]; then
        ZT_EFFECTIVE_PORT="$(cat "${ZT_PORT_FILE}")"
    else
        ZT_EFFECTIVE_PORT="${ZT_PORT}"
        echo "${ZT_EFFECTIVE_PORT}" > "${ZT_PORT_FILE}"
    fi

    zerotier-one -p"${ZT_EFFECTIVE_PORT}" -d || exit 1

    export Providers__ZeroTier__Port="${ZT_EFFECTIVE_PORT}"

    cd "${APP_PATH}"
    exec dotnet HomeMesh.WebApi.dll
}

check_zerotier
start
