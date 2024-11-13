# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["WebApplicationApi/WebApplicationApi.csproj", "WebApplicationApi/"]
RUN dotnet restore "WebApplicationApi/WebApplicationApi.csproj"

# Copy everything else and build
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

# Set environment variables for PostgreSQL and production settings
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1

# Expose the application's port (default ASP.NET Core port)
EXPOSE 80

# Run the application
ENTRYPOINT ["dotnet", "WebApplicationApi.dll"]
