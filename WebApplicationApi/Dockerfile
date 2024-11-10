# Step 1: Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["WebApplicationApi/WebApplicationApi.csproj", "WebApplicationApi/"]
RUN dotnet restore "WebApplicationApi/WebApplicationApi.csproj"
COPY . .
WORKDIR "/src/WebApplicationApi"
RUN dotnet build "WebApplicationApi.csproj" -c Release -o /app/build

# Step 2: Publish Stage
FROM build AS publish
RUN dotnet publish "WebApplicationApi.csproj" -c Release -o /app/publish

# Step 3: Final Stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY ["WebApplicationApi/DokumentationssystemDB.db3", "/app/Data/"]

# Set permissions for the SQLite database file
RUN chmod -R 777 /app/Data/DokumentationssystemDB.db3

# Entry point for the application
ENTRYPOINT ["dotnet", "WebApplicationApi.dll"]
