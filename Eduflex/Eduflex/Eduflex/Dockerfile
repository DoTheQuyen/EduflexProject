## See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.
#
## Depending on the operating system of the host machines(s) that will build or run the containers, the image specified in the FROM statement may need to be changed.
## For more information, please see https://aka.ms/containercompat
#
## This stage is used when running from VS in fast mode (Default for Debug configuration)
#FROM mcr.microsoft.com/dotnet/aspnet:8.0-nanoserver-1809 AS base
#WORKDIR /app
#EXPOSE 8080
#EXPOSE 8081
#
#
## This stage is used to build the service project
#FROM mcr.microsoft.com/dotnet/sdk:8.0-nanoserver-1809 AS build
#ARG BUILD_CONFIGURATION=Release
#WORKDIR /src
#COPY ["Eduflex/Eduflex.csproj", "Eduflex/"]
#RUN dotnet restore "./Eduflex/Eduflex.csproj"
#COPY . .
#WORKDIR "/src/Eduflex"
#RUN dotnet build "./Eduflex.csproj" -c %BUILD_CONFIGURATION% -o /app/build
#
## This stage is used to publish the service project to be copied to the final stage
#FROM build AS publish
#ARG BUILD_CONFIGURATION=Release
#RUN dotnet publish "./Eduflex.csproj" -c %BUILD_CONFIGURATION% -o /app/publish /p:UseAppHost=false
#
## This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
#FROM base AS final
#WORKDIR /app
#COPY --from=publish /app/publish .
#ENTRYPOINT ["dotnet", "Eduflex.dll"]


# Multi-stage build for optimized image size
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["Eduflex.API.csproj", "."]
RUN dotnet restore "Eduflex.API.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src"
RUN dotnet build "Eduflex.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Eduflex.csproj" -c Release -o /app/publish

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Create a non-root user for security
RUN groupadd -r eduflex && useradd -r -g eduflex eduflex
RUN chown -R eduflex:eduflex /app
USER eduflex

# Copy published application
COPY --from=publish --chown=eduflex:eduflex /app/publish .

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:80/health || exit 1

# Expose port
EXPOSE 80
EXPOSE 443

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:80

# Entry point
ENTRYPOINT ["dotnet", "Eduflex.API.dll"]