FROM mcr.microsoft.com/dotnet/aspnet:8.0-windowsservercore-ltsc2019 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


FROM mcr.microsoft.com/dotnet/sdk:8.0-windowsservercore-ltsc2019 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["WebApplicationApi/WebApplicationApi.csproj", "WebApplicationApi/"]
RUN dotnet restore "./WebApplicationApi/WebApplicationApi.csproj"
COPY . .
WORKDIR "/src/WebApplicationApi"
RUN dotnet build "./WebApplicationApi.csproj" -c %BUILD_CONFIGURATION% -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./WebApplicationApi.csproj" -c %BUILD_CONFIGURATION% -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WebApplicationApi.dll"]