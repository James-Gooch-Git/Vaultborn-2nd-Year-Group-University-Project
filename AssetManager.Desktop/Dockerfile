# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

COPY ./Infrastructure/*.csproj ./Infrastructure/
WORKDIR /app/Infrastructure
RUN dotnet restore

COPY ./Infrastructure/ ./Infrastructure/

# Runtime Stage
RUN dotnet publish -c Release -o /app/out
FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app

COPY --from=build-env /app/out ./

# API port
EXPOSE 5000

# entry point
ENTRYPOINT ["dotnet", "Infrastructure.dll"]
