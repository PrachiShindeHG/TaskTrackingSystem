# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# === BASE RUNTIME (LOCAL ONLY) ===
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# === BUILD STAGE (LOCAL ONLY) ===
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# COPY PROJECT FILES (INCLUDING TaskService REFERENCE)
COPY ["ReportingService/ReportingService.csproj", "ReportingService/"]
COPY ["TaskService/TaskService.csproj", "TaskService/"]

# RESTORE USING LOCAL SDK — NO INTERNET PULL
RUN dotnet restore "./ReportingService/ReportingService.csproj"

# COPY SOURCE CODE
COPY . .

# BUILD REPORTINGSERVICE
WORKDIR "/src/ReportingService"
RUN dotnet build "./ReportingService.csproj" -c $BUILD_CONFIGURATION -o /app/build

# === PUBLISH ===
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ReportingService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# === FINAL ===
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ReportingService.dll"]