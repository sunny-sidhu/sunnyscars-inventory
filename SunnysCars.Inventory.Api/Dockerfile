# Build from the repo root so global.json is in the context:
#   docker build --platform linux/amd64 -f SunnysCars.Inventory.Api/Dockerfile -t sunnyscars-inventory-api .
# App Service on Linux runs amd64, so always pass --platform linux/amd64 (an arm64 Mac would otherwise build arm64).

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
# The base image listens on ASPNETCORE_HTTP_PORTS=8080. TLS is terminated in front of the container.
EXPOSE 8080

# The SDK runs on the build machine's own architecture and cross-compiles for the target, instead of running under emulation.
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
ARG TARGETARCH
WORKDIR /src
COPY ["global.json", "./"]
COPY ["SunnysCars.Inventory.Api/SunnysCars.Inventory.Api.csproj", "SunnysCars.Inventory.Api/"]
RUN dotnet restore "SunnysCars.Inventory.Api/SunnysCars.Inventory.Api.csproj" -a $TARGETARCH
COPY ["SunnysCars.Inventory.Api/", "SunnysCars.Inventory.Api/"]
RUN dotnet publish "SunnysCars.Inventory.Api/SunnysCars.Inventory.Api.csproj" -a $TARGETARCH --no-restore \
    -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SunnysCars.Inventory.Api.dll"]
