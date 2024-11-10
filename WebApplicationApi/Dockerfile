FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["WebApplicationApi/WebApplicationApi.csproj", "WebApplicationApi/"]
RUN dotnet restore "WebApplicationApi/WebApplicationApi.csproj"
COPY . .
WORKDIR "/src/WebApplicationApi"
RUN dotnet build "WebApplicationApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "WebApplicationApi.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY ["WebApplicationApi/DokumentationssystemDB.db3", "/data/"]
ENTRYPOINT ["dotnet", "WebApplicationApi.dll"]
