FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["WebApplicationApi/WebApplicationApi.csproj", "WebApplicationApi/"]
RUN dotnet restore "WebApplicationApi/WebApplicationApi.csproj"
COPY . .
WORKDIR "/src/WebApplicationApi"
RUN dotnet publish "WebApplicationApi.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
# Copy the SQLite database file to the container
COPY WebApplicationApi/DokumentationssystemDB.db3 .

ENTRYPOINT ["dotnet", "WebApplicationApi.dll"]