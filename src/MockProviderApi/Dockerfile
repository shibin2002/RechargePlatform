# Multi-stage build for MockProviderApi
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy solution and projects
COPY RechargePlatform.sln ./
COPY src/RechargePlatform.Common/ src/RechargePlatform.Common/
COPY src/MockProviderApi/ src/MockProviderApi/

# Restore and build
RUN dotnet restore src/MockProviderApi/MockProviderApi.csproj
RUN dotnet publish src/MockProviderApi/MockProviderApi.csproj -c Release -o /app/publish /p:UseAppHost=false

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "MockProviderApi.dll"]
