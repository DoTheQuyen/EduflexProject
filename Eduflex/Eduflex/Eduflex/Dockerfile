# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy everything
COPY . .

# Restore using the solution file
RUN dotnet restore Eduflex/Eduflex/Eduflex.sln

# Mark we are building in Docker (skips swagger)
ENV BuildingInsideDocker=true

# Publish the API project
WORKDIR /src/Eduflex/Eduflex/Eduflex
RUN dotnet publish -c Release -o /app/publish -p:BuildingInsideDocker=true

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# ✅ Force ASP.NET to listen on port 80
ENV ASPNETCORE_URLS=http://+:80

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Eduflex.dll"]

# ✅ Document exposed port (must match containerPort in ECS task def)
EXPOSE 80
