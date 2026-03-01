# ── Stage 1: Build ─────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

# Copy solution and project files first so Docker can cache the restore layer
COPY ProcessManager.sln .
COPY src/ProcessManager.Domain/ProcessManager.Domain.csproj  src/ProcessManager.Domain/
COPY src/ProcessManager.Api/ProcessManager.Api.csproj        src/ProcessManager.Api/
COPY src/ProcessManager.Web/ProcessManager.Web.csproj        src/ProcessManager.Web/
COPY tests/ProcessManager.Tests/ProcessManager.Tests.csproj  tests/ProcessManager.Tests/

RUN dotnet restore src/ProcessManager.Web/ProcessManager.Web.csproj

# Copy source and publish
COPY src/ProcessManager.Domain/ src/ProcessManager.Domain/
COPY src/ProcessManager.Api/    src/ProcessManager.Api/
COPY src/ProcessManager.Web/    src/ProcessManager.Web/

RUN dotnet publish src/ProcessManager.Web/ProcessManager.Web.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ── Stage 2: Runtime ────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

# Render injects PORT=10000; fall back to 10000 for local runs.
ENV ASPNETCORE_URLS=""
EXPOSE 10000

ENTRYPOINT ["sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-10000} dotnet ProcessManager.Web.dll"]
