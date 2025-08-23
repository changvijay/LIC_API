# Dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY *.sln .
COPY LIC_WebDeskAPI/*.csproj ./LIC_WebDeskAPI/
RUN dotnet restore

COPY LIC_WebDeskAPI/. ./LIC_WebDeskAPI/
WORKDIR /src/LIC_WebDeskAPI

# Publish for linux-x64 so native assets are Linux-compatible
RUN dotnet publish -c Release -r linux-x64 --self-contained false -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Ensure the app listens on the Cloud Run port
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "LIC_WebDeskAPI.dll"]