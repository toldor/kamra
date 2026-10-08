# Kamra - one Dockerfile, three targets (ADR-0005, ADR-0006):
#   migrator - applies EF Core migrations once (EF migration bundle), then exits
#   api      - ASP.NET Core Api serving the built React SPA from wwwroot (same origin)

# ---- Frontend: React SPA build (output goes to the Api's wwwroot) ----
FROM node:24-alpine AS frontend
WORKDIR /src/src/frontend
COPY src/frontend/package.json src/frontend/package-lock.json ./
RUN npm ci
COPY src/frontend/ ./
RUN npm run build

# ---- Backend: restore, publish and migration bundle ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
# Project files first, so the restore layer is cached until a dependency changes.
COPY global.json Directory.Build.props Directory.Packages.props dotnet-tools.json .editorconfig ./
COPY src/backend/KamraApp.Domain/KamraApp.Domain.csproj src/backend/KamraApp.Domain/
COPY src/backend/KamraApp.Application/KamraApp.Application.csproj src/backend/KamraApp.Application/
COPY src/backend/KamraApp.Infrastructure/KamraApp.Infrastructure.csproj src/backend/KamraApp.Infrastructure/
COPY src/backend/KamraApp.Api/KamraApp.Api.csproj src/backend/KamraApp.Api/
RUN dotnet restore src/backend/KamraApp.Api/KamraApp.Api.csproj && dotnet tool restore
COPY src/backend/ src/backend/
COPY --from=frontend /src/src/backend/KamraApp.Api/wwwroot src/backend/KamraApp.Api/wwwroot
RUN dotnet publish src/backend/KamraApp.Api/KamraApp.Api.csproj -c Release -o /app/publish --no-restore
# Design time only needs a syntactically valid connection string; the bundle gets the real one at run time.
RUN ConnectionStrings__Default="Host=localhost;Database=design-time" \
    dotnet ef migrations bundle -p src/backend/KamraApp.Infrastructure -s src/backend/KamraApp.Api \
    --configuration Release -o /app/efbundle

# ---- Migrator: runs the bundle against ConnectionStrings__Default ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS migrator
WORKDIR /app
COPY --from=build /app/efbundle ./efbundle
USER $APP_UID
ENTRYPOINT ["/bin/sh", "-c", "exec ./efbundle --connection \"$ConnectionStrings__Default\""]

# ---- Api ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS api
WORKDIR /app
# Data Protection keys (cookie and antiforgery encryption) live on a volume; the directory is created
# here owned by the non-root app user, so a new named volume inherits a writable owner.
RUN mkdir /keys && chown $APP_UID /keys
COPY --from=build /app/publish ./
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "KamraApp.Api.dll"]
