# Step 1: Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the project file and restore dependencies
COPY ["WebApplicationApi/WebApplicationApi.csproj", "WebApplicationApi/"]
RUN dotnet restore "WebApplicationApi/WebApplicationApi.csproj"

# Copy the rest of the application files
COPY . .
WORKDIR "/src/WebApplicationApi"
RUN dotnet build "WebApplicationApi.csproj" -c Release -o /app/build

# Step 2: Publish Stage
FROM build AS publish
RUN dotnet publish "WebApplicationApi.csproj" -c Release -o /app/publish

# Step 3: Final Stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Copy published files
COPY --from=publish /app/publish .

# Create the data directory and set permissions
RUN mkdir -p /data && chmod -R 777 /data

# Set the connection string to point to the new path in your application
ENV DATABASE_PATH="Data/DokumentationssystemDB.db3"

# Entry point for the application
ENTRYPOINT ["dotnet", "WebApplicationApi.dll"]
