# syntax=docker/dockerfile:1
# Build context is the `backend/` directory:
#   docker build -f src/VirtualWardrobe.Api/Dockerfile -t vw-api backend/

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore as a cached layer: copy the shared MSBuild props + the project graph first.
COPY Directory.Build.props ./
COPY src/VirtualWardrobe.Domain/VirtualWardrobe.Domain.csproj src/VirtualWardrobe.Domain/
COPY src/VirtualWardrobe.Application/VirtualWardrobe.Application.csproj src/VirtualWardrobe.Application/
COPY src/VirtualWardrobe.Infrastructure/VirtualWardrobe.Infrastructure.csproj src/VirtualWardrobe.Infrastructure/
COPY src/VirtualWardrobe.Api/VirtualWardrobe.Api.csproj src/VirtualWardrobe.Api/
RUN dotnet restore src/VirtualWardrobe.Api/VirtualWardrobe.Api.csproj

# Copy the rest of the source and publish.
COPY src/ src/
RUN dotnet publish src/VirtualWardrobe.Api/VirtualWardrobe.Api.csproj \
    -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Listen on 8080 (Caddy reverse-proxies /api to api:8080).
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY --from=build /app/publish ./

# Run as the non-root user provided by the .NET base image.
USER app

ENTRYPOINT ["dotnet", "VirtualWardrobe.Api.dll"]
