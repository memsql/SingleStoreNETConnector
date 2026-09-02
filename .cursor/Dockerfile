# Development environment for the SingleStore .NET Connector.
#
# The .NET 10 SDK is pinned because CI targets net10.0 (see
# .github/workflows/config.yml) and this single SDK can build every framework
# the connector multi-targets (net462/471/48, netstandard2.0/2.1, net6.0/8.0/9.0/10.0)
# by restoring reference packs on demand.
FROM mcr.microsoft.com/dotnet/sdk:10.0

ENV DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_NOLOGO=1 \
    DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

# git and curl support source control and tooling; the MySQL/MariaDB client is
# used by the repository's SingleStore integration-test helper scripts under
# .github/workflows and .ci.
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        ca-certificates \
        curl \
        git \
        default-mysql-client \
    && rm -rf /var/lib/apt/lists/*
