FROM node:22-bookworm-slim AS node-runtime

FROM mcr.microsoft.com/dotnet/sdk:latest AS webapi-publish

COPY --from=node-runtime /usr/local/ /usr/local/

WORKDIR /src

COPY HomeMesh.sln ./
COPY Directory.Build.props ./
COPY src/HomeMesh.Abstractions/HomeMesh.Abstractions.csproj src/HomeMesh.Abstractions/
COPY src/HomeMesh.AdminConsole/package.json src/HomeMesh.AdminConsole/
COPY src/HomeMesh.AdminConsole/package-lock.json src/HomeMesh.AdminConsole/
COPY src/HomeMesh.Application/HomeMesh.Application.csproj src/HomeMesh.Application/
COPY src/HomeMesh.Domain/HomeMesh.Domain.csproj src/HomeMesh.Domain/
COPY src/HomeMesh.Infrastructure/HomeMesh.Infrastructure.csproj src/HomeMesh.Infrastructure/
COPY src/HomeMesh.Protocol.ZeroTier/HomeMesh.Protocol.ZeroTier.csproj src/HomeMesh.Protocol.ZeroTier/
COPY src/HomeMesh.WebApi/HomeMesh.WebApi.csproj src/HomeMesh.WebApi/

RUN dotnet restore src/HomeMesh.WebApi/HomeMesh.WebApi.csproj --verbosity minimal

COPY src ./src

RUN dotnet publish src/HomeMesh.WebApi/HomeMesh.WebApi.csproj \
    --configuration Release \
    --output /out \
    --no-restore \
    --verbosity minimal

RUN test -f /out/HomeMesh.WebApi.dll \
    && test -f /out/wwwroot/index.html

FROM mcr.microsoft.com/dotnet/sdk:latest AS mkworld-builder

ARG UBUNTU_MIRROR=

RUN printf 'Acquire::http::Pipeline-Depth "0";\nAcquire::Retries "5";\n' \
        > /etc/apt/apt.conf.d/99no-pipeline

RUN if [ -f /etc/apt/sources.list.d/ubuntu.sources ]; then \
        sed -i \
            -e 's|http://archive.ubuntu.com/ubuntu|https://archive.ubuntu.com/ubuntu|g' \
            -e 's|http://security.ubuntu.com/ubuntu|https://security.ubuntu.com/ubuntu|g' \
            /etc/apt/sources.list.d/ubuntu.sources; \
    fi; \
    if [ -f /etc/apt/sources.list ]; then \
        sed -i \
            -e 's|http://archive.ubuntu.com/ubuntu|https://archive.ubuntu.com/ubuntu|g' \
            -e 's|http://security.ubuntu.com/ubuntu|https://security.ubuntu.com/ubuntu|g' \
            /etc/apt/sources.list; \
    fi; \
    if [ -n "${UBUNTU_MIRROR}" ] && [ -f /etc/apt/sources.list.d/ubuntu.sources ]; then \
        sed -i \
            -e "s|http://archive.ubuntu.com/ubuntu|${UBUNTU_MIRROR}|g" \
            -e "s|https://archive.ubuntu.com/ubuntu|${UBUNTU_MIRROR}|g" \
            -e "s|http://security.ubuntu.com/ubuntu|${UBUNTU_MIRROR}|g" \
            -e "s|https://security.ubuntu.com/ubuntu|${UBUNTU_MIRROR}|g" \
            /etc/apt/sources.list.d/ubuntu.sources; \
    fi; \
    if [ -n "${UBUNTU_MIRROR}" ] && [ -f /etc/apt/sources.list ]; then \
        sed -i \
            -e "s|http://archive.ubuntu.com/ubuntu|${UBUNTU_MIRROR}|g" \
            -e "s|https://archive.ubuntu.com/ubuntu|${UBUNTU_MIRROR}|g" \
            -e "s|http://security.ubuntu.com/ubuntu|${UBUNTU_MIRROR}|g" \
            -e "s|https://security.ubuntu.com/ubuntu|${UBUNTU_MIRROR}|g" \
            /etc/apt/sources.list; \
    fi

RUN apt-get update && DEBIAN_FRONTEND=noninteractive apt-get install -y --no-install-recommends \
        build-essential cmake curl ca-certificates \
    && rm -rf /var/lib/apt/lists/*

COPY deploy/docker/zt-sparse/node /zt/node
COPY deploy/docker/zt-sparse/osdep /zt/osdep
COPY deploy/docker/zt-sparse/include /zt/include
COPY deploy/docker/zt-sparse/attic /zt/attic
COPY deploy/docker/zt-sparse/ext/nlohmann /zt/ext/nlohmann

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

FROM mcr.microsoft.com/dotnet/aspnet:10.0

ARG TZ=Asia/Shanghai
ARG DEBIAN_MIRROR=
ARG DEBIAN_SECURITY_MIRROR=
ARG UBUNTU_MIRROR=
ENV TZ=${TZ}
ENV ASPNETCORE_URLS=http://+:8080

RUN printf 'Acquire::http::Pipeline-Depth "0";\nAcquire::Retries "5";\n' \
        > /etc/apt/apt.conf.d/99no-pipeline; \
    if [ -f /etc/apt/sources.list.d/ubuntu.sources ]; then \
        sed -i \
            -e 's|http://archive.ubuntu.com/ubuntu|https://archive.ubuntu.com/ubuntu|g' \
            -e 's|http://security.ubuntu.com/ubuntu|https://security.ubuntu.com/ubuntu|g' \
            /etc/apt/sources.list.d/ubuntu.sources; \
    fi; \
    if [ -f /etc/apt/sources.list.d/debian.sources ]; then \
        sed -i \
            -e 's|http://deb.debian.org/debian|https://deb.debian.org/debian|g' \
            -e 's|http://security.debian.org/debian-security|https://deb.debian.org/debian-security|g' \
            /etc/apt/sources.list.d/debian.sources; \
    fi; \
    if [ -f /etc/apt/sources.list ]; then \
        sed -i \
            -e 's|http://deb.debian.org/debian|https://deb.debian.org/debian|g' \
            -e 's|http://security.debian.org/debian-security|https://deb.debian.org/debian-security|g' \
            -e 's|http://archive.ubuntu.com/ubuntu|https://archive.ubuntu.com/ubuntu|g' \
            -e 's|http://security.ubuntu.com/ubuntu|https://security.ubuntu.com/ubuntu|g' \
            /etc/apt/sources.list; \
    fi; \
    if [ -n "${UBUNTU_MIRROR}" ] && [ -f /etc/apt/sources.list.d/ubuntu.sources ]; then \
        sed -i \
            -e "s|http://archive.ubuntu.com/ubuntu|${UBUNTU_MIRROR}|g" \
            -e "s|https://archive.ubuntu.com/ubuntu|${UBUNTU_MIRROR}|g" \
            -e "s|http://security.ubuntu.com/ubuntu|${UBUNTU_MIRROR}|g" \
            -e "s|https://security.ubuntu.com/ubuntu|${UBUNTU_MIRROR}|g" \
            /etc/apt/sources.list.d/ubuntu.sources; \
    fi; \
    if [ -n "${DEBIAN_MIRROR}" ] && [ -f /etc/apt/sources.list.d/debian.sources ]; then \
        sed -i \
            -e "s|http://deb.debian.org/debian|${DEBIAN_MIRROR}|g" \
            -e "s|https://deb.debian.org/debian|${DEBIAN_MIRROR}|g" \
            /etc/apt/sources.list.d/debian.sources; \
    fi; \
    if [ -n "${DEBIAN_SECURITY_MIRROR}" ] && [ -f /etc/apt/sources.list.d/debian.sources ]; then \
        sed -i \
            -e "s|http://security.debian.org/debian-security|${DEBIAN_SECURITY_MIRROR}|g" \
            -e "s|https://security.debian.org/debian-security|${DEBIAN_SECURITY_MIRROR}|g" \
            /etc/apt/sources.list.d/debian.sources; \
    fi; \
    if [ -n "${DEBIAN_MIRROR}" ] && [ -f /etc/apt/sources.list ]; then \
        sed -i \
            -e "s|http://deb.debian.org/debian|${DEBIAN_MIRROR}|g" \
            -e "s|https://deb.debian.org/debian|${DEBIAN_MIRROR}|g" \
            /etc/apt/sources.list; \
    fi; \
    if [ -n "${DEBIAN_SECURITY_MIRROR}" ] && [ -f /etc/apt/sources.list ]; then \
        sed -i \
            -e "s|http://security.debian.org/debian-security|${DEBIAN_SECURITY_MIRROR}|g" \
            -e "s|https://security.debian.org/debian-security|${DEBIAN_SECURITY_MIRROR}|g" \
            /etc/apt/sources.list; \
    fi; \
    if [ -n "${UBUNTU_MIRROR}" ] && [ -f /etc/apt/sources.list ]; then \
        sed -i \
            -e "s|http://archive.ubuntu.com/ubuntu|${UBUNTU_MIRROR}|g" \
            -e "s|https://archive.ubuntu.com/ubuntu|${UBUNTU_MIRROR}|g" \
            -e "s|http://security.ubuntu.com/ubuntu|${UBUNTU_MIRROR}|g" \
            -e "s|https://security.ubuntu.com/ubuntu|${UBUNTU_MIRROR}|g" \
            /etc/apt/sources.list; \
    fi

RUN apt-get update \
    && DEBIAN_FRONTEND=noninteractive apt-get install -y --no-install-recommends \
        tzdata ca-certificates curl jq iproute2 \
    && rm -rf /var/lib/apt/lists/*

RUN curl -fsSL https://install.zerotier.com | bash; \
    test -x /usr/sbin/zerotier-one || { echo "ZeroTier install failed"; exit 1; }; \
    rm -rf /var/lib/zerotier-one/*

COPY --from=mkworld-builder /zt/attic/world/mkworld /usr/local/bin/mkworld
RUN chmod +x /usr/local/bin/mkworld

RUN mkdir -p /app/data/artifacts /var/lib/zerotier-one

COPY --from=webapi-publish /out /app/

COPY deploy/docker/entrypoint.sh /app/entrypoint.sh
RUN chmod +x /app/entrypoint.sh

WORKDIR /app

EXPOSE 8080
EXPOSE 9993/udp
EXPOSE 9993

ENTRYPOINT ["/bin/sh", "/app/entrypoint.sh"]
