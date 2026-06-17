# syntax=docker/dockerfile:1

# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy only the project files first so `dotnet restore` is cached unless a
# .csproj changes (layer caching). Mirror the solution's project layout.
COPY src/Xenoh.Domain/Xenoh.Domain.csproj         src/Xenoh.Domain/
COPY src/Xenoh.Application/Xenoh.Application.csproj src/Xenoh.Application/
COPY src/Xenoh.Infrastructure/Xenoh.Infrastructure.csproj src/Xenoh.Infrastructure/
COPY src/Xenoh.API/Xenoh.API.csproj               src/Xenoh.API/
RUN dotnet restore src/Xenoh.API/Xenoh.API.csproj

# Copy the rest of the source and publish a Release build.
COPY . .
RUN dotnet publish src/Xenoh.API/Xenoh.API.csproj -c Release -o /app/publish --no-restore

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Fonts for server-side PR share-card rendering (SixLabors.Fonts). The base image
# ships with none, which makes SystemFonts.Families empty and crashes text drawing.
# fonts-dejavu-core provides /usr/share/fonts/truetype/dejavu/DejaVuSans.ttf.
RUN apt-get update \
    && apt-get install -y --no-install-recommends fontconfig fonts-dejavu-core \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

# Listen on plain HTTP inside the container (TLS is terminated upstream in prod;
# locally Prometheus scrapes this directly over the Docker network).
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

ENTRYPOINT ["dotnet", "Xenoh.API.dll"]
