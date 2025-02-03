# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

COPY *.csproj ./
RUN dotnet restore

COPY . .

# Runtime Stage
RUN dotnet publish -c Release -o /app/out
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

COPY --from=build-env /app/out .

# API port
EXPOSE 5000

# entry point
ENTRYPOINT ["dotnet", "AssetManager.Infrastructure.dll"]
