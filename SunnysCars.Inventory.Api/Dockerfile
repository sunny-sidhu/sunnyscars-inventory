FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["SunnysCars.Inventory.Api/SunnysCars.Inventory.Api.csproj", "SunnysCars.Inventory.Api/"]
RUN dotnet restore "SunnysCars.Inventory.Api/SunnysCars.Inventory.Api.csproj"
COPY . .
WORKDIR "/src/SunnysCars.Inventory.Api"
RUN dotnet build "./SunnysCars.Inventory.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./SunnysCars.Inventory.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SunnysCars.Inventory.Api.dll"]
