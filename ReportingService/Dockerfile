# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# === BASE RUNTIME (LOCAL ONLY) ===
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# === BUILD STAGE (LOCAL ONLY) ===
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy only .csproj files first (cache this layer)
COPY ["ReportingService/ReportingService.csproj", "ReportingService/"]
COPY ["TaskService/TaskService.csproj", "TaskService/"]
RUN dotnet restore "./ReportingService/ReportingService.csproj"

# Copy source files (only invalidates if source changes)
COPY ["ReportingService/", "ReportingService/"]
COPY ["TaskService/", "TaskService/"]

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