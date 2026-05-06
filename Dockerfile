# syntax=docker/dockerfile:1.7
#
# Two-stage build that publishes the Blazor WASM client and the API server, then
# composes them into a single ASP.NET Core runtime image. The API serves the WASM
# from /wwwroot and the upload disk from /wwwroot/uploads (mounted as a volume in
# compose).

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Pin the SDK against global.json so rollForward + version selection match local dev.
COPY global.json Directory.Build.props ./

# Copy csproj files first so dotnet restore can layer-cache. Adding a new package
# anywhere invalidates this layer; touching source code does not.
COPY src/Needlr.Domain/Needlr.Domain.csproj             src/Needlr.Domain/
COPY src/Needlr.Application/Needlr.Application.csproj   src/Needlr.Application/
COPY src/Needlr.Infrastructure/Needlr.Infrastructure.csproj src/Needlr.Infrastructure/
COPY src/Needlr.Contracts/Needlr.Contracts.csproj       src/Needlr.Contracts/
COPY src/Needlr.Api/Needlr.Api.csproj                   src/Needlr.Api/
COPY src/Needlr.Web/Needlr.Web.csproj                   src/Needlr.Web/

RUN dotnet restore src/Needlr.Api/Needlr.Api.csproj
RUN dotnet restore src/Needlr.Web/Needlr.Web.csproj

COPY src/ src/

# Publish the WASM client first; its output lives under /publish/web/wwwroot and gets
# overlaid onto the API's wwwroot in the runtime stage.
RUN dotnet publish src/Needlr.Web/Needlr.Web.csproj \
    -c Release \
    -o /publish/web \
    --no-restore

RUN dotnet publish src/Needlr.Api/Needlr.Api.csproj \
    -c Release \
    -o /publish/api \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime

# Run as a non-root user so a container compromise can't write outside /app + the
# mounted upload volume.
RUN groupadd -g 10001 needlr \
 && useradd  -u 10001 -g 10001 -m -d /home/needlr needlr

WORKDIR /app

COPY --from=build --chown=needlr:needlr /publish/api          ./
COPY --from=build --chown=needlr:needlr /publish/web/wwwroot  ./wwwroot/

# Match Caddy's reverse-proxy upstream port. Production-only — dev still runs Kestrel
# on its launch-profile defaults via `dotnet run`.
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_USE_POLLING_FILE_WATCHER=false

EXPOSE 8080
USER needlr

ENTRYPOINT ["dotnet", "Needlr.Api.dll"]
