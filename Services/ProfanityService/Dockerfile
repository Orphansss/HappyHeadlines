# build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["ProfanityService.csproj", "./"]
RUN dotnet restore "ProfanityService.csproj"

COPY . .
RUN dotnet publish "ProfanityService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ProfanityService.dll"]
