FROM mcr.microsoft.com/devcontainers/dotnet:1-9.0-bookworm

# LibreOffice headless + fonts + rendering deps
# Single RUN layer, apt lists deleted to keep image lean
RUN apt-get update && apt-get install -y --no-install-recommends \
    libreoffice-nogui \
    fonts-liberation \
    fonts-dejavu \
    libfontconfig1 \
    libgdiplus \
    && rm -rf /var/lib/apt/lists/*

# Node.js LTS
RUN curl -fsSL https://deb.nodesource.com/setup_lts.x | bash - \
    && apt-get install -y nodejs \
    && rm -rf /var/lib/apt/lists/*

# Blazor WASM workload
RUN dotnet workload install wasm-tools

ENV ASPNETCORE_ENVIRONMENT=Development
ENV DOTNET_WATCH_RESTART_ON_RUDE_EDIT=true
ENV ASPNETCORE_URLS=http://+:5000
