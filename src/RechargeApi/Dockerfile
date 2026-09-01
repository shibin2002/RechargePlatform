# Multi-stage build for RechargeApi
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy solution and projects
COPY RechargePlatform.sln ./
COPY src/RechargePlatform.Common/ src/RechargePlatform.Common/
COPY src/RechargePlatform.Data/ src/RechargePlatform.Data/
COPY src/RechargeApi/ src/RechargeApi/
COPY src/MockProviderApi/ src/MockProviderApi/

# Restore and build
RUN dotnet restore src/RechargeApi/RechargeApi.csproj
RUN dotnet publish src/RechargeApi/RechargeApi.csproj -c Release -o /app/publish /p:UseAppHost=false

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "RechargeApi.dll"]
