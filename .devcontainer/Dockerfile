FROM mcr.microsoft.com/devcontainers/dotnet:8.0

# Install Mono properly for .NET Framework support
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
    apt-transport-https \
    dirmngr \
    gnupg \
    ca-certificates \
    && apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF \
    && echo "deb https://download.mono-project.com/repo/debian stable-buster main" | tee /etc/apt/sources.list.d/mono-official-stable.list \
    && apt-get update \
    && apt-get install -y --no-install-recommends \
    mono-complete \
    mono-dbg \
    mono-runtime-dbg \
    mono-devel \
    nuget \
    ca-certificates-mono \
    && rm -rf /var/lib/apt/lists/*

# Verify Mono installation
RUN mono --version

# Verify .NET SDK versions are available
RUN dotnet --list-sdks

# Set up script for running .NET Framework apps with Mono
RUN echo '#!/bin/bash\nmono "$@"' > /usr/local/bin/dotnet-mono \
    && chmod +x /usr/local/bin/dotnet-mono

# Add labels
LABEL description="Development container for Serilog.Sinks.Datadog.Logs" 