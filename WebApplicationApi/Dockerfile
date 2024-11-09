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

# Use the persistent volume path for the database
COPY ["WebApplicationApi/DokumentationssystemDB.db3", "/data/DokumentationssystemDB.db3"]

# Set the connection string to point to the new path in your application
ENV DATABASE_PATH="/data/DokumentationssystemDB.db3"

ENTRYPOINT ["dotnet", "WebApplicationApi.dll"]
