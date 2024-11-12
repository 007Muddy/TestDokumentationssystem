# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["WebApplicationApi/WebApplicationApi.csproj", "WebApplicationApi/"]
RUN dotnet restore "WebApplicationApi/WebApplicationApi.csproj"
COPY . .
WORKDIR "/src/WebApplicationApi"
RUN dotnet build "WebApplicationApi.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish "WebApplicationApi.csproj" -c Release -o /app/publish

# Stage 3: Final image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Copy published output to the final container
COPY --from=publish /app/publish .

# Set environment variables to Production
ENV ASPNETCORE_ENVIRONMENT=Production

# Set up SQL Server connection string
version: '3.4'

services:
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      SA_PASSWORD: "YourStrong@Passw0rd"
      ACCEPT_EULA: "Y"
    ports:
      - "1433:1433"

  webapplicationapi:
    build: .
    environment:
      ConnectionStrings__DefaultConnection: "Server=sqlserver;Database=DokumentationssystemDB;"
    ports:
      - "5000:80"
    depends_on:
      - sqlserver

# Run the application
ENTRYPOINT ["dotnet", "WebApplicationApi.dll"]

