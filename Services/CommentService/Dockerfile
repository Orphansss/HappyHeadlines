# syntax=docker/dockerfile:1

# ---- runtime base ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
ENV ASPNETCORE_URLS=http://0.0.0.0:8081
EXPOSE 8081

# ---- build ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# copy project files (restore layer)
COPY Services/CommentService/CommentService.csproj Services/CommentService/
COPY Infrastructure/Monitoring/Monitoring.csproj       Infrastructure/Monitoring/
RUN dotnet restore Services/CommentService/CommentService.csproj

# copy the rest
COPY . .
WORKDIR /src/Services/CommentService
RUN dotnet build CommentService.csproj -c $BUILD_CONFIGURATION -o /app/build --no-restore

# ---- publish ----
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
WORKDIR /src/Services/CommentService
RUN dotnet publish CommentService.csproj -c $BUILD_CONFIGURATION -o /app/publish --no-restore /p:UseAppHost=false

# ---- final ----
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet","CommentService.dll"]
