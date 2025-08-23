FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files for caching (csproj is at repo root)
COPY *.sln ./
COPY *.csproj ./
RUN dotnet restore

# Copy the rest of the source
COPY . ./

# Publish the project for linux-x64 so native assets are Linux-compatible
RUN dotnet publish ./LIC_WebDeskAPI.csproj -c Release -r linux-x64 --self-contained false -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Ensure the app listens on the Cloud Run port
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "LIC_WebDeskAPI.dll"]